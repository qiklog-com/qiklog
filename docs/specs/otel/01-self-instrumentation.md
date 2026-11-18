# Stage 1 — Instrument QikLog itself

Branch: `feat/otel-self-instrumentation`, from updated `main` after stage 0.

Program decisions: [README](README.md).

## Problem

When an ingest fails or slows down, the only evidence is a log line and a
Prometheus counter. There is no span from validate to SignalR, and no way to
point QikLog at Jaeger without code changes.

## Scope

- Traces for `QikLog.Api` only, registered only when OTLP export is configured.
- An `ActivitySource` named `QikLog.Ingest` with four spans on `POST /v1/logs`.
- Npgsql, ASP.NET Core, and `HttpClient` trace instrumentation on that same
  switch.
- `docs/OBSERVABILITY.md` and a Compose profile that runs Jaeger.
- Tests listed below.

## Out of scope

- OTLP log ingest (stage 2+).
- Metrics. Do not change `QikLogMetrics`, `UseHttpMetrics`, or `MapMetrics`.
- An OTLP log or metric exporter.
- `QikLog:OpenTelemetry` (or any other) appsettings switch. Env vars only.
- Tracing `QikLog.Web`, the CLI, or the Serilog sink.
- A www doc. Customers do not configure this. Operators read
  `docs/OBSERVABILITY.md`.
- Changing `POST /v1/logs` status codes or response bodies.

## Packages

On `QikLog.Api`:

- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `Npgsql.OpenTelemetry`

On `QikLog.Api.Tests` only: `OpenTelemetry.Exporter.InMemory`.

## Behavior

### When tracing is off

Tracing is off when either:

- `OTEL_EXPORTER_OTLP_ENDPOINT` is missing or whitespace, or
- `OTEL_SDK_DISABLED` is `true` (case-insensitive).

Then `Program` does not call `AddOpenTelemetry`. No `TracerProvider`, no OTLP
exporter, no Npgsql trace instrumentation. `POST /v1/logs` behaves as it does
today. `StartActivity` has no listener, returns null, and the handler must
tolerate that.

Read the endpoint from the process environment, which is what
`UseOtlpExporter()` reads. Do not add a config key. Tests that flip the
variable use a non-parallel xUnit collection, set it before the host builds,
and clear it afterward so other `WebApplicationFactory` tests are not affected.

### When tracing is on

Register the SDK and `UseOtlpExporter()` so protocol, headers, timeout, and
endpoint come from the standard `OTEL_*` variables. Do not set the endpoint in
code.

`WithTracing` adds:

- ASP.NET Core instrumentation, without an enricher that copies headers or
  bodies.
- `HttpClient` instrumentation whose filter drops requests whose URI starts
  with `OTEL_EXPORTER_OTLP_ENDPOINT`. That stops the exporter's own POST from
  becoming another span that gets exported.
- `AddNpgsql()`.
- Source `QikLog.Ingest`.

No `WithMetrics`. No log exporter. Default resource `service.name` is
`qiklog-api` when `OTEL_SERVICE_NAME` is unset.

If the stock ASP.NET Core instrumentation records `url.query` or `url.full`,
add a processor that strips those attributes. The API key is allowed in the
query string today (`?api_key=`). It must not leave the process on a span.

### Spans

`ActivitySource` name: `QikLog.Ingest`. Four sibling internal spans, children
of the current activity (the ASP.NET Core server span when that
instrumentation is on). Names, in order, started only if the step runs:

| Span | Wraps | Not started when |
|------|--------|------------------|
| `ingest.validate` | source and message checks, building the `LogEntry` | never, for a request that entered the handler |
| `ingest.usage_check` | `CheckIngestAllowedAsync` | validation failed |
| `ingest.persist` | `SaveAsync` | validation failed or usage denied |
| `ingest.broadcast` | SignalR `LogReceived` | validation failed, usage denied, or persist threw |

Tags on every span that started, and nowhere else:

| Tag | Value |
|-----|--------|
| `source.name` | Trimmed source. Omit the tag when the source is missing or whitespace. |
| `tenant.id` | `ITenantContext.TenantId` in `D` format (`00000000-0000-0000-0000-000000000000`). Omit when there is no tenant. |

No other QikLog tags. Do not put the message, properties, API key, or
`Authorization` value on a tag, event, link, baggage item, or status
description.

Status:

- Usage denied: `ingest.usage_check` is `Error`. The handler still returns 402.
- Validation failed: `ingest.validate` is `Error`. The handler still returns 400.
- Persist or broadcast throws: that span is `Error` and the exception
  propagates. Do not swallow it to keep a trace pretty.
- Success: status unset or `Ok`. Not `Error`.

Npgsql may add children under `ingest.persist` when the store hits Postgres.
The in-memory test does not require those children.

## Jaeger

`docs/OBSERVABILITY.md` is the operator doc. It must show:

1. `docker compose --profile observability up -d` brings up Jaeger. The
   profile is not started by `make up-d`.
2. Jaeger all-in-one, UI on port 16686, OTLP HTTP on 4318.
3. The api service does **not** get `OTEL_EXPORTER_OTLP_ENDPOINT` by default.
   The doc shows how to set, on the api container only:

   ```text
   OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4318
   OTEL_SERVICE_NAME=qiklog-api
   ```

4. One `POST /v1/logs`, then in the Jaeger UI a trace whose QikLog spans are
   `ingest.validate`, `ingest.usage_check`, `ingest.persist`,
   `ingest.broadcast`.
5. A warning: do not point `OTEL_EXPORTER_OTLP_ENDPOINT` at this API. QikLog
   does not accept traces, and a later log exporter aimed at `/otlp` would
   loop. The HTTP filter is the backstop for the exporter's own calls.

## Conditions of Satisfaction

1. With `OTEL_EXPORTER_OTLP_ENDPOINT` unset, the host starts, `POST /v1/logs`
   still returns 202, and no `TracerProvider` is registered.
2. With `OTEL_SDK_DISABLED=true` and an endpoint set, same as COS 1.
3. With an endpoint set, a `TracerProvider` is registered and the SDK is
   configured with `UseOtlpExporter()` (standard env vars, no QikLog endpoint
   setting).
4. A successful ingest produces the four spans, in that order, tagged with
   `tenant.id` and `source.name`.
5. No span attribute, event, baggage item, or status text contains the API
   key or the message body. A key passed as `?api_key=` is included in that
   ban.
6. A 400 ingest has only `ingest.validate`, status `Error`.
7. A 402 ingest has `ingest.validate` and `ingest.usage_check`
   (`Error`), and no persist or broadcast span.
8. `HttpClient` instrumentation drops requests under the configured OTLP
   endpoint.
9. `GET /metrics` still responds. `QikLogMetrics` is unchanged.
10. `docs/OBSERVABILITY.md` and the Compose profile exist and match the
    Jaeger section above. Default `make up-d` does not start Jaeger and does
    not set the endpoint.

## Scenarios

Project: `tests/QikLog.Api.Tests`, except scenario 8, which is a unit test of
the filter in that same project. Method names are the headings. First lines
of each body: `// Given:`, `// When:`, `// Then:`.

### 1. `Given_otlp_endpoint_unset_When_api_starts_Then_no_tracer_provider_and_ingest_returns_202`

Covers COS 1.

- **Given** the OTLP endpoint env var is unset and the test host did not add
  an exporter
- **When** the API host starts and a client POSTs a valid `/v1/logs` body
- **Then** the response is 202 and `TracerProvider` is not registered

### 2. `Given_sdk_disabled_When_endpoint_set_Then_no_tracer_provider`

Covers COS 2.

- **Given** `OTEL_SDK_DISABLED=true` and `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- **When** the host starts
- **Then** `TracerProvider` is not registered

### 3. `Given_otlp_endpoint_set_When_api_starts_Then_tracer_provider_is_registered`

Covers COS 3.

- **Given** `OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:9` and the SDK is
  not disabled
- **When** the host starts
- **Then** `TracerProvider` is registered
- **And** the test does not read a `QikLog:*` tracing key because none exists

### 4. `Given_in_memory_exporter_When_ingest_succeeds_Then_span_tree_has_tenant_and_source_and_no_secrets`

Covers COS 4 and 5.

- **Given** an in-memory exporter listening to `QikLog.Ingest`, a known API
  key, source `checkout-api`, message `payment captured secret-body-marker`,
  and a resolved tenant
- **When** the client POSTs `/v1/logs` with `Authorization: Bearer {key}`
- **Then** the exported `QikLog.Ingest` spans are exactly `ingest.validate`,
  `ingest.usage_check`, `ingest.persist`, `ingest.broadcast`, started in that
  order
- **And** each carries `source.name=checkout-api` and `tenant.id` equal to
  the tenant guid in `D` format
- **And** no attribute, event, baggage, or status description on any span
  (including the server span, when present) contains the API key or
  `secret-body-marker`

### 5. `Given_in_memory_exporter_When_api_key_is_in_query_Then_key_is_not_on_any_span`

Covers COS 5.

- **Given** the same exporter and a valid body
- **When** the client POSTs `/v1/logs?api_key={key}` with a valid body
- **Then** the response is 202
- **And** no captured span contains the key

### 6. `Given_in_memory_exporter_When_source_missing_Then_only_validate_span_is_error`

Covers COS 6.

- **Given** the in-memory exporter
- **When** the client POSTs `/v1/logs` without a source
- **Then** the response is 400
- **And** the only `QikLog.Ingest` span is `ingest.validate` with status
  `Error`
- **And** `source.name` is absent

### 7. `Given_in_memory_exporter_When_usage_denied_Then_no_persist_span`

Covers COS 7.

- **Given** the in-memory exporter and a free-tier cap already reached (same
  setup as `UsageLimitTests`)
- **When** the client POSTs a valid `/v1/logs` body
- **Then** the response is 402
- **And** the `QikLog.Ingest` spans are `ingest.validate` and
  `ingest.usage_check`
- **And** `ingest.usage_check` has status `Error`
- **And** there is no `ingest.persist` or `ingest.broadcast` span

### 8. `Given_otlp_endpoint_When_request_targets_it_Then_http_filter_drops_the_span`

Covers COS 8.

- **Given** endpoint `http://jaeger:4318`
- **When** the filter is asked about
  `http://jaeger:4318/v1/traces` and about `https://example.com/v1/logs`
- **Then** the exporter URL is dropped and the other URL is kept

### 9. `Given_tracing_on_When_scraping_metrics_Then_metrics_endpoint_still_returns_200`

Covers COS 9.

- **Given** an endpoint is set so tracing is on
- **When** the client GETs `/metrics`
- **Then** the response is 200 and the body still contains
  `qiklog_logs_ingested_total`

COS 10 is the doc and the Compose profile. Review them; do not assert the
Jaeger UI from `make test`.
