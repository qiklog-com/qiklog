# Stage 4 — Accept OTLP logs over gRPC

Branch: `feat/otlp-grpc`, from `feat/otlp-http-protobuf`.

Program decisions: [README](README.md). Do not write the service, the tests,
or the package reference until the gate below passes.

## Problem

Some exporters speak OTLP gRPC only, or default to it
(`OTEL_EXPORTER_OTLP_PROTOCOL=grpc`). HTTP on `/otlp/v1/logs` does not serve
those clients. gRPC only works if the public edge delivers HTTP/2 to the api
container. Building it for local Compose and then discovering Railway strips
HTTP/2 is wasted work.

## Gate

Before any code, confirm that Railway's edge supports inbound gRPC to the
**api** service: HTTP/2 end to end, trailers preserved, no downgrade to
HTTP/1.1 between the edge and the container.

Evidence is a Railway doc citation that covers this service's proxy, or a
live probe Jamey has approved. Ambiguous docs are a failed gate. Do not
deploy a probe on your own.

If the gate fails or is unproven: stop. Report what you found. Do not add
grpc-web, a local-only listener, a second public port, or an HTTP/1.1
bridge.

Azure Container Apps is not part of this gate. It is open question 3 in the
README. Do not block on it, and do not treat an ACA guess as a Railway pass.

## Scope, only if the gate passes

- `opentelemetry.proto.collector.logs.v1.LogsService/Export` via
  `Grpc.AspNetCore`, on the API host root. Not under `/otlp`.
- The stage 3 adapter and the stage 2 mapper. No third copy of the mapping.
- API-key auth from gRPC metadata, same header names.
- Over cap: `RESOURCE_EXHAUSTED`.
- In-process gRPC client tests.
- A short addition to the www OTLP page.

## Out of scope

- `TraceService` and `MetricsService`.
- Changing HTTP/JSON or HTTP/protobuf behavior.
- grpc-web.
- Moving `POST /v1/logs` or `POST /otlp/v1/logs` onto a different port.
- A customer-facing requirement to turn on HTTP/2 for the JSON API. JSON
  stays HTTP/1.1-capable.

## Behavior

Enable HTTP/1 and HTTP/2 on the existing Kestrel endpoint. Do not add a port.

`Export`:

- Metadata keys, case-insensitive: `X-QikLog-API-Key`, `X-Api-Key`,
  `Authorization` (`Bearer`). Same validation as
  `TenantAuthenticationService`. The query string does not exist on this
  transport; do not invent one.
- Missing credentials: `UNAUTHENTICATED`.
- Invalid, revoked, or unknown tenant: `PERMISSION_DENIED`.
- Per-key rate limit, one token per `Export`: `RESOURCE_EXHAUSTED` with
  detail `rate limit exceeded`.
- Monthly cap denial: `RESOURCE_EXHAUSTED` with the same detail text as HTTP
  402 (`monthly ingest limit exceeded — upgrade to Pro`, or
  `tenant context required`). Nothing is persisted. This code is retryable
  in the OTLP spec; that retry risk is accepted unless open question 2 is
  answered otherwise.
- Malformed request that gRPC itself rejects stays the framework status.
  Records with empty bodies follow stage 2 partial success: status `OK`,
  `partial_success.rejected_log_records` set, `error_message` containing
  `empty body`, good records persisted.
- Full success: `OK` and an empty `ExportLogsServiceResponse`.
- Message size: `MaxReceiveMessageSize` equals
  `QikLog:Otlp:MaxDecompressedBodyBytes`. A larger message is not persisted.
  Assert the call is not `OK` and history is unchanged; record the framework
  status in the test once, do not catch it and continue.
- Mapping, caps, `service.name`, groups, and `LogsIngested` match stage 2.
  One usage check per `Export`, counted by accepted records.

Customer configuration, for the www page:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=https://api.qiklog.com
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=X-QikLog-API-Key=ql_your_full_key_here
```

The endpoint is the API origin, not `https://api.qiklog.com/otlp`. Say that
explicitly so nobody reuses the HTTP base URL.

When enforcement is off, `Export` does not require a key, same as the HTTP
routes.

## Docs

Add a gRPC section to `www/src/content/docs/opentelemetry.md` with the env
vars above and one sentence that HTTP/protobuf on `/otlp` is unchanged.
`make www-build`.

If the gate fails, do not mention gRPC as available. No www edit.

## Conditions of Satisfaction

1. The gate was checked and recorded in the PR before the service was added.
   A failed gate means this list is void and no package was added.
2. An in-process client `Export` of one record returns `OK`, and history
   shows that message under `service.name`.
3. Missing metadata is `UNAUTHENTICATED`. A bad key is `PERMISSION_DENIED`.
   Neither stores a row.
4. A batch that does not fit the monthly cap is `RESOURCE_EXHAUSTED` with
   detail containing `monthly ingest limit exceeded`, and stores nothing.
5. One empty body beside one good record is `OK` with
   `rejected_log_records == 1` and only the good record stored.
6. HTTP `POST /otlp/v1/logs` and `POST /v1/logs` still pass their existing
   tests. The stage 2 mapper was not copied.
7. The www page documents the gRPC endpoint as the API origin.

## Scenarios

Project: `tests/QikLog.Api.Tests`, in-process `GrpcChannel` to the test
host. Do not duplicate the stage 2 mapping matrix. Mapping is already
covered in Core and in the stage 3 parser tests.

#### 1. `Given_in_process_client_When_exporting_one_record_Then_history_contains_it`

Covers COS 2.

- **Given** a key in metadata `X-QikLog-API-Key` and one record with
  `service.name` `checkout-api` and a unique message
- **When** the client calls `Export`
- **Then** the status is `OK`, `partial_success` is unset, and history for
  `checkout-api` contains the message

#### 2. `Given_no_metadata_When_exporting_Then_unauthenticated`

Covers COS 3.

- **Given** enforcement on and no API-key metadata
- **When** the client calls `Export`
- **Then** the status is `UNAUTHENTICATED` and history does not contain the
  message

#### 3. `Given_invalid_key_When_exporting_Then_permission_denied`

Covers COS 3.

- **Given** metadata with a key that fails validation
- **When** the client calls `Export`
- **Then** the status is `PERMISSION_DENIED` and nothing is stored

#### 4. `Given_cap_reached_When_exporting_Then_resource_exhausted_and_nothing_stored`

Covers COS 4.

- **Given** the free cap is already reached
- **When** the client exports one valid record
- **Then** the status is `RESOURCE_EXHAUSTED`, the detail contains
  `monthly ingest limit exceeded`, and the message is not in history

#### 5. `Given_one_empty_body_When_exporting_two_records_Then_partial_success`

Covers COS 5.

- **Given** one record `kept` and one with an empty body
- **When** the client exports them
- **Then** the status is `OK`, `rejected_log_records` is 1, `error_message`
  contains `empty body`, and only `kept` is stored

#### 6. `Given_message_over_max_receive_size_When_exporting_Then_not_stored`

Covers the size rule.

- **Given** `MaxDecompressedBodyBytes` is 4096 and a message larger than that
- **When** the client calls `Export`
- **Then** the status is not `OK` and history does not contain the message

COS 1 is the PR write-up, not a unit test. COS 6 is `make test` including
the stage 2 and 3 suites. COS 7 is `make www-build`, and only if the gate
passed.
