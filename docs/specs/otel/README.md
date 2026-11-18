# OpenTelemetry for QikLog

Logs only. QikLog will emit its own traces, then accept customer logs over OTLP.
It will not ingest traces or metrics.

This folder is the contract for that work. Stage 0 is these documents and nothing
else. Later stages implement one file each, in order, and stop for review.

Anchored to `main` at the time this was written:

- `POST /v1/logs` validates source and message, calls `IUsageLimitService`,
  returns **402** for any denial, then `ILogEntryStore.SaveAsync` and a SignalR
  `LogReceived` on `tenant:{id}:source:{name}` or `source:{name}`. Success is
  **202** with an empty body.
- Auth is `TenantAuthMiddleware` + `TenantAuthenticationService`. Missing key
  is **401**, invalid or revoked key is **403**, per-key rate limit is **429**.
  Headers: `X-QikLog-API-Key`, `X-Api-Key`, `Authorization: Bearer`, and the
  `api_key` query string. Rate limiting runs only when the path starts with
  `/v1/logs`.
- `UsageLimitService` counts persisted `LogEntries` since UTC month start and
  denies when `count >= limit`. The options comment says "requests"; the query
  counts rows. One `POST /v1/logs` is one row, so the two match today.
- Metrics stay on prometheus-net: `GET /metrics` and `QikLogMetrics`. No OTLP
  metrics exporter.
- Enforcement is on in `QikLogApiWebApplicationFactory`
  (`QikLog:AuthEnforcement:Enabled=true`). Local Compose defaults it off.

## Stages

| Stage | Branch | Spec | Ships |
|------:|--------|------|-------|
| 0 | `docs/otel-specs` | this folder | Specs only |
| 1 | `feat/otel-self-instrumentation` | [01-self-instrumentation.md](01-self-instrumentation.md) | Traces for QikLog's own ingest |
| 2 | `feat/otlp-http-json` | [02-otlp-http-json.md](02-otlp-http-json.md) | `POST /otlp/v1/logs` JSON |
| 3 | `feat/otlp-http-protobuf` | [03-otlp-http-protobuf.md](03-otlp-http-protobuf.md) | Same route, `application/x-protobuf` |
| 4 | `feat/otlp-grpc` | [04-otlp-grpc.md](04-otlp-grpc.md) | gRPC `LogsService/Export`, or a stop |

Stacking: stage 1 branches from updated `main` after stage 0 is in `main`.
Stages 2, 3, and 4 each branch from the previous stage's branch. One PR per
stage. Never push to `main`.

Each implementation stage: failing tests first (xUnit + Shouldly, names and
`// Given:` / `// When:` / `// Then:` comments from the scenarios here), then
`make test` green, then small Conventional Commits. See `docs/TESTING.md`.

## Decisions

These are locked. A later stage does not re-trade them unless this file changes
in the same PR.

1. **Logs only.** No `/otlp/v1/traces`, no `/otlp/v1/metrics`, no trace or
   metric storage. Those paths stay 404.
2. **Do not take over `/v1/logs`.** OTLP exporters append `/v1/logs` to a base
   URL. Customers set `OTEL_EXPORTER_OTLP_ENDPOINT=https://api.qiklog.com/otlp`
   (or `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` to the full path). The route is
   `POST /otlp/v1/logs`.
3. **Self-instrumentation is traces only**, and only when
   `OTEL_EXPORTER_OTLP_ENDPOINT` is set. No `QikLog:*` key turns tracing on.
   prometheus-net stays the metrics path. No OTLP log exporter on QikLog.Api,
   so the API cannot ship its own logs back into itself.
4. **Span tags are `tenant.id` and `source.name`.** Never an API key, the
   `Authorization` header, the `api_key` query, or a message body.
5. **Auth for OTLP is the existing API-key path**, including 401 vs 403.
   Recommend `X-QikLog-API-Key` in `OTEL_EXPORTER_OTLP_HEADERS` because a
   Bearer token contains a space that must be percent-encoded. Bearer still
   works. The query-string key still works because it is the same helper;
   user docs will not mention it.
6. **Monthly cap counts accepted log records, not HTTP calls.**
   `POST /v1/logs` still passes `1`. An OTLP batch that does not fit entirely
   is **402 for the whole request** and persists nothing. Rejected records
   (empty body) are not charged. The check stays racy under concurrency, same
   as today; no new lock.
7. **Rate limit stays per call**, not per record. One token for each
   `POST /v1/logs`, each `POST /otlp/v1/logs`, and each gRPC `Export`. Default
   remains 120/minute/key. Extend the path check; `/otlp/v1/logs` does not
   start with `/v1/logs`.
8. **OTLP success is HTTP 200**, body `ExportLogsServiceResponse`. Not 202.
   `POST /v1/logs` stays 202. Error responses keep the existing JSON shape
   `{ "error": "..." }` so 402 can still return `usage` and `limit`.
9. **Partial success is HTTP 200** with `partialSuccess.rejectedLogRecords`
   and a non-empty `errorMessage`. The only per-record rejection is a missing
   or whitespace body. Everything else maps or is omitted (bad trace id,
   over-long key, extra properties).
10. **One mapper.** JSON and protobuf both become an internal model in
    `QikLog.Core`. `QikLog.Core` does not reference protobuf. The mapper is
    the only place that knows severity bands, `service.name`, property caps,
    and hex ids.
11. **Property caps** (configurable, these defaults): 64 properties, key
    length 256 (drop the property), value length 4096 (truncate). Synthetics
    win over record attributes, which win over `resource.*`. `service.name`
    becomes `LogEntry.Source` and is not copied to `resource.service.name`.
    Missing or blank `service.name` becomes source `unknown_service`.
12. **Body limits** (configurable): 1 MiB on the wire, 4 MiB after gzip.
    Either overrun is **413** and persists nothing.
13. **gRPC, if it is built, is the collector service on the API host root**
    (`/opentelemetry.proto.collector.logs.v1.LogsService/Export`), not under
    `/otlp`. HTTP/JSON and HTTP/protobuf stay on `POST /otlp/v1/logs`.
14. **Stage 4 stops** if Railway's edge does not deliver HTTP/2 to the api
    container. No grpc-web, no sidecar, no second public port.

## Out of scope for the whole program

- Trace and metric ingest, browsing traces in the dashboard, trace-to-log
  correlation UI.
- Changing `POST /v1/logs` status codes, JSON shape, or auth.
- A QikLog log exporter, Serilog changes, or `QikLog.Extensions.Logging`.
- Billing, new plans, Stripe, per-source retention.
- Deduping retried batches. A retry is another ingest, same as `/v1/logs`.
- Instrumenting `QikLog.Web` or `QikLog.Cli`.
- New NuGet packages beyond the ones named in the stage specs.

## Packages named by stage

Stage 1, `QikLog.Api`:

- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `Npgsql.OpenTelemetry`

Stage 1 tests only: `OpenTelemetry.Exporter.InMemory`. The work order asks for
an in-memory exporter; that is this package. It does not ship in the API.

Stage 3: `Grpc.Tools`, plus `Google.Protobuf` because the generated code does
not compile without it. See open questions.

Stage 4, only if the gate passes: `Grpc.AspNetCore`.

Pin versions in the csproj at implementation time. No floating versions.

## Open questions

Answer before the stage that needs them. The spec's recommendation is what
gets built if you just say go.

1. **Batch that straddles the cap.** A tenant with one slot left sends two
   valid records. This spec returns 402 and stores nothing.
   *Alternative:* store one, partial-success the other. Say so before stage 2
   if you want that. Recommendation: keep the atomic 402.
2. **gRPC quota status.** You asked for `RESOURCE_EXHAUSTED` when over cap.
   The OTLP spec treats that code as retryable, so exporters will keep
   sending a batch that can never fit. Recommendation: keep
   `RESOURCE_EXHAUSTED` as requested, and put the monthly-cap text in the
   status detail so it is distinguishable from the per-minute rate limit
   (same code, different detail). Switch to `FAILED_PRECONDITION` only if you
   want exporters to stop.
3. **Azure Container Apps.** The stage 4 gate is Railway, which is the live
   edge. ACA is the planned host. Recommendation: do not block stage 4 on ACA,
   but do not cut DNS to ACA until inbound HTTP/2 to the api container is
   confirmed there too.
4. **`Google.Protobuf`.** Not in the original package list. Stage 3 needs it.
   Recommendation: allow it, pinned, next to `Grpc.Tools`.
5. **Cap numbers.** 64 / 256 / 4096 and 1 MiB / 4 MiB are defaults, not
   measurements. Change the table in
   [02-otlp-http-json.md](02-otlp-http-json.md) before stage 2 if you want
   different ones.
6. **Proto tag.** Pinned at the start of stage 3 to the current stable
   `open-telemetry/opentelemetry-proto` release, Apache-2.0, recorded beside
   the vendored files. Not frozen in this document.
