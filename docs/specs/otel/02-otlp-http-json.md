# Stage 2 — Accept OTLP logs over HTTP/JSON

Branch: `feat/otlp-http-json`, from `feat/otel-self-instrumentation`.

Program decisions: [README](README.md). This stage locks the mapping. Stage 3
reuses it. Do not copy it into a second implementation.

## Problem

.NET apps and the OpenTelemetry Collector speak OTLP. QikLog only accepts its
own `POST /v1/logs` JSON. Operators should point a stock OTLP log exporter at
QikLog and see lines in live tail.

## Scope

- `POST /otlp/v1/logs`, `Content-Type: application/json`.
- gzip, body caps, the existing API-key auth and rate limit, monthly cap per
  accepted record.
- A pure mapper from one internal record to `LogEntry`.
- Persist and SignalR through the same store, hub, and group names as
  `POST /v1/logs`.
- End-user doc `www/src/content/docs/opentelemetry.md`.

## Out of scope

- `application/x-protobuf` (stage 3). A protobuf `Content-Type` is **415**.
- gRPC (stage 4).
- `/v1/logs` request or response changes, other than the usage check growing
  a record count that `/v1/logs` calls with `1`.
- Trace or metric routes.
- New Prometheus metrics. Reuse `LogsIngested` and `UsageLimitChecks`.
- Deduping, batch transactions, or a new usage lock.
- Nested objects inside `LogEntry.Properties`. Values stay strings.

## Request

```http
POST /otlp/v1/logs
Content-Type: application/json
```

`application/json; charset=utf-8` is JSON. Anything else, including
`application/x-protobuf`, is **415** `{ "error": "unsupported content type" }`.

`Content-Encoding: gzip` is accepted (case-insensitive). Identity (header
absent) is accepted. Anything else is **415**
`{ "error": "unsupported content encoding" }`. A gzip payload that does not
decompress is **400** `{ "error": "malformed gzip body" }`.

### Body size

| Key | Default | Env |
|-----|--------:|-----|
| `QikLog:Otlp:MaxRequestBodyBytes` | 1048576 | `QikLog__Otlp__MaxRequestBodyBytes` |
| `QikLog:Otlp:MaxDecompressedBodyBytes` | 4194304 | `QikLog__Otlp__MaxDecompressedBodyBytes` |
| `QikLog:Otlp:MaxProperties` | 64 | `QikLog__Otlp__MaxProperties` |
| `QikLog:Otlp:MaxPropertyKeyLength` | 256 | `QikLog__Otlp__MaxPropertyKeyLength` |
| `QikLog:Otlp:MaxPropertyValueLength` | 4096 | `QikLog__Otlp__MaxPropertyValueLength` |

The wire cap applies to the bytes read from the socket, compressed or not.
Set this endpoint's max request body to `MaxRequestBodyBytes` so Kestrel's
30 MB default is not the real limit. The decompress cap applies while
inflating, not after a full buffer. Crossing either cap is **413**
`{ "error": "request body too large" }` and persists nothing.

### Auth and tenancy

`ProtectedApiRoutes`: `POST /otlp/v1/logs` is `AuthMode.ApiKey`, same as
`POST /v1/logs`.

`TryApiKeyAsync` applies the ingest rate limit to this path too. One token
per HTTP request. The 121st call in the window is **429**
`{ "error": "rate limit exceeded" }`.

401, 403, and the header names stay exactly what
`TenantAuthenticationService` already does. User docs tell people to set:

```text
OTEL_EXPORTER_OTLP_HEADERS=X-QikLog-API-Key=ql_your_full_key_here
```

Bearer works if the space is encoded
(`Authorization=Bearer%20ql_your_full_key_here`). Docs mention that once, and
recommend the `X-QikLog-API-Key` form.

When auth enforcement is off, the route accepts anonymous calls, same as
`/v1/logs`.

### Usage

After mapping, let `accepted` be the records that produced a `LogEntry`.

Extend the usage check with a positive record count. `POST /v1/logs` passes
1, and every existing usage test still passes. Denial rule:
`count + recordCount > limit` (for `recordCount == 1` this is today's
`count >= limit`).

- `accepted` is 0: do not call the usage check.
- The check denies: **402** with `{ "error", "usage", "limit" }` using the
  same reason strings as today (`monthly ingest limit exceeded — upgrade to Pro`
  or `tenant context required`). Persist nothing. Do not return
  `partialSuccess`. `usage` is the current row count, not `count + accepted`.
- The check allows: persist every accepted record. Each one increments
  `qiklog_logs_ingested_total`. `UsageLimitChecks` increments once per
  request, label `allowed` or `denied`, not once per record.

Empty bodies are not charged and are not persisted.

### JSON shape

OTLP/JSON, proto3 JSON mapping:

- Field names are lowerCamelCase (`resourceLogs`, `scopeLogs`, `logRecords`,
  `timeUnixNano`, `observedTimeUnixNano`, `severityNumber`, `severityText`,
  `traceId`, `spanId`, `partialSuccess`, `rejectedLogRecords`,
  `errorMessage`).
- Unknown fields are ignored.
- 64-bit integers may be JSON numbers or strings. Accept both for
  `timeUnixNano`, `observedTimeUnixNano`, and `intValue`.
- Enums are integers. A string `severityNumber` does not select a band; treat
  it as unspecified (0) and fall through to `severityText`.
- `traceId` and `spanId` are hex strings, not base64.

A body that is not JSON, or whose root is not a JSON object, is **400**
`{ "error": "malformed OTLP JSON" }`. `{}` and `{ "resourceLogs": [] }` are
valid empty exports. `{ "resourceLogs": null }` is **400**.

Minimal record:

```json
{
  "resourceLogs": [{
    "resource": {
      "attributes": [
        { "key": "service.name", "value": { "stringValue": "checkout-api" } }
      ]
    },
    "scopeLogs": [{
      "scope": { "name": "Checkout.Logger" },
      "logRecords": [{
        "timeUnixNano": "1710000000000000000",
        "severityNumber": 9,
        "severityText": "INFO",
        "body": { "stringValue": "payment captured" },
        "attributes": [
          { "key": "orderId", "value": { "stringValue": "ord_123" } }
        ],
        "traceId": "4BF92F3577B34DA6A3CE929D0E0E4736",
        "spanId": "00F067AA0BA902B7"
      }]
    }]
  }]
}
```

`AnyValue` keys: `stringValue`, `boolValue`, `intValue`, `doubleValue`,
`bytesValue` (base64), `arrayValue.values`, `kvlistValue.values`.

### Precedence

1. Middleware: 401, 403, 429.
2. Content-Type and Content-Encoding.
3. Body size, then gzip.
4. JSON parse.
5. Map each record.
6. If `accepted > 0` and usage denies: 402, persist nothing.
7. If `accepted == 0` and at least one record was rejected: 200 partial
   success, persist nothing.
8. If the export is empty: 200 `{}`.
9. Otherwise persist accepted records in document order (resource, then
   scope, then record) and return 200. Include `partialSuccess` only when a
   record was rejected.

## Mapping

The internal model and the mapper live in `QikLog.Core`. The mapper is a pure
function of `(record, receivedAt)`. It does not read the clock, the database,
or the HTTP request. `receivedAt` is UTC.

### Source

`service.name` on the resource, trimmed. Missing, blank, or non-string:
`unknown_service`. It is not also stored as `resource.service.name`.

### Level

`SeverityNumber` bands:

| Number | `LogLevel` |
|-------:|------------|
| 1–4 | Trace |
| 5–8 | Debug |
| 9–12 | Info |
| 13–16 | Warning |
| 17–20 | Error |
| 21–24 | Critical |

`0`, omitted, or any other number: parse `SeverityText`, case-insensitive.

| Text | `LogLevel` |
|------|------------|
| `trace` | Trace |
| `debug` | Debug |
| `info`, `information` | Info |
| `warning`, `warn` | Warning |
| `error`, `err` | Error |
| `critical`, `crit`, `fatal` | Critical |

Unrecognized or missing text: Info. A number in range wins over
`severityText`.

### Message

String body as-is, including surrounding spaces that are not *only*
whitespace. Non-string bodies become a string:

| Body | Message |
|------|---------|
| bool | `true` or `false` |
| int | invariant decimal, no group separators |
| double | invariant round-trip (`G17`) |
| bytes | standard base64 |
| array or kvlist | compact JSON, UTF-8, no extra whitespace |

Missing body, empty string, or whitespace-only string: reject the record.
Do not persist it. `errorMessage` on the response contains `empty body`.

### Timestamp

`timeUnixNano` when present and greater than 0. Else
`observedTimeUnixNano` when present and greater than 0. Else `receivedAt`.

A string that is not an integer, or a value `DateTimeOffset` cannot
represent, is treated as absent and the next source is tried. Do not reject
the record.

### Properties

Flat `IReadOnlyDictionary<string, string>`. Build in this order, then apply
caps:

1. Synthetics, when they have a value: `trace_id`, `span_id`,
   `severity_text`, `scope.name`.
2. Record attributes, in encounter order.
3. Other resource attributes as `resource.{key}`, in encounter order.
   Skip `service.name`.

Rules:

- `trace_id`: exactly 32 hex digits, stored lowercase. Any other value,
  including base64, is omitted. The record is still accepted.
- `span_id`: exactly 16 hex digits, stored lowercase. Same omit rule.
- `severity_text`: the raw text when it is non-empty after trim.
- `scope.name`: instrumentation scope name when non-empty after trim.
- Attribute values use the same scalar and JSON rules as the message body.
  Nested array and kvlist values are JSON strings, not nested dictionaries.
- A key longer than `MaxPropertyKeyLength` is dropped.
- A value longer than `MaxPropertyValueLength` is truncated to that many
  chars. No ellipsis.
- A synthetic key overwrites a record attribute of the same name.
- After that, if more than `MaxProperties` remain, keep the earliest in the
  order above and drop the rest.
- `flags` and `droppedAttributesCount` are ignored.
- Null or empty properties on the `LogEntry`: store null, matching the
  current store's empty-dictionary behavior. If the cap pass leaves zero
  properties, the dictionary is null.

### Persist and tail

For each accepted entry, `SaveAsync` then `LogReceived`, using
`LogHubGroups.ForTenantSource` when the tenant is set and `ForSource`
otherwise. A batch with two `service.name` values is two sources and two
groups. A failure from the store or the hub propagates; earlier entries in
the batch stay saved. No new transaction.

When stage 1 tracing is on, the OTLP handler starts the same four span names
once per request, not once per record.

- `source.name` is set only when every accepted record shares one source.
- `otlp.accepted_records` is the accepted count.
- Same ban on keys and message bodies.
- When tracing is off, the handler does not require a listener.

Do not refactor `POST /v1/logs` except to pass `1` into the usage check if
the signature requires it. Sharing a private "save and broadcast" helper is
fine. Duplicating the mapper is not.

## Response

Success and partial success: **200**, `Content-Type: application/json`,
uncompressed.

Full success, including an empty export:

```json
{}
```

Partial success:

```json
{
  "partialSuccess": {
    "rejectedLogRecords": 1,
    "errorMessage": "rejected 1 log record: empty body"
  }
}
```

`rejectedLogRecords` is a JSON number. `errorMessage` is non-empty and
contains `empty body` whenever every rejection in that response was an empty
body. The count is the number of rejected records, not the number of
accepted ones.

Error bodies are the QikLog shape, not `google.rpc.Status`:

| Status | Body |
|-------:|------|
| 400 | `{ "error": "malformed OTLP JSON" }` or `{ "error": "malformed gzip body" }` |
| 401 / 403 / 429 | unchanged from the middleware |
| 402 | `{ "error", "usage", "limit" }` unchanged from `/v1/logs` |
| 413 | `{ "error": "request body too large" }` |
| 415 | `{ "error": "unsupported content type" }` or `{ "error": "unsupported content encoding" }` |

## Docs

Add `www/src/content/docs/opentelemetry.md`:

- Frontmatter `title`: `Send logs with OpenTelemetry`. `order`: 10.
- Honest about pre-alpha: this page ships with the feature, not before.
- Endpoint table: local `http://localhost:5080/otlp`, hosted
  `https://api.qiklog.com/otlp`. Exporters append `/v1/logs`.
- Header: `OTEL_EXPORTER_OTLP_HEADERS=X-QikLog-API-Key=...`.
- Protocol on this page: `OTEL_EXPORTER_OTLP_PROTOCOL=http/json`. Do not tell
  anyone to send protobuf until stage 3.
- .NET example: `OTEL_SERVICE_NAME`, the endpoint, the protocol, the header.
  No new QikLog NuGet package. The stock OpenTelemetry logging exporter.
- Collector `otlphttp` example with `endpoint: https://api.qiklog.com/otlp`,
  the API-key header, gzip, and JSON encoding. Confirm the collector field
  name against current collector docs while writing the page; the expected
  key is `encoding: json`. Say that the collector's protobuf default is not
  accepted yet.
- Caps table, the 402-for-the-whole-batch rule, and `unknown_service`.
- Next steps linking `/docs/ingest-api/`, `/docs/api-keys/`, and
  `/docs/live-tail/`.
- One paragraph on the ingest API page pointing here. Do not change the
  landing page.

`make www-build` when the page is added.

## Conditions of Satisfaction

1. `POST /v1/logs` status codes, bodies, and auth are unchanged. Passing `1`
   to the usage check still denies at `count >= limit`.
2. JSON `POST /otlp/v1/logs` maps a record as specified: source, every
   severity band, severity text fallback, non-string bodies, timestamps,
   trace and span ids, scope name, `resource.` prefix, caps.
3. Missing API key is 401. Invalid key is 403. Rate limit is 429, once per
   request.
4. A batch that does not fit the remaining monthly cap returns 402 and
   persists nothing. A batch that fits charges one cap unit per accepted
   record.
5. gzip is accepted. Over the wire cap or the decompressed cap is 413.
   Malformed JSON is 400. Wrong content type is 415.
6. One empty body in a batch of two returns 200, `rejectedLogRecords` 1,
   `errorMessage` containing `empty body`, and only the good record is
   stored.
7. Accepted records are in history for that source and are broadcast with
   `LogReceived` on the same group `/v1/logs` would use.
8. Spans, when tracing is on, do not contain the API key or any message body.
9. The www page exists, builds, and shows JSON, not protobuf, as the
   protocol.

## Scenarios

Mapper scenarios are pure tests in `tests/QikLog.Core.Tests`. They call the
mapper with a fixed `receivedAt` of `2026-01-15T00:00:00Z`. API scenarios use
`QikLogApiWebApplicationFactory` in `tests/QikLog.Api.Tests`. Names are the
test methods.

### Mapping

#### 1. `Given_severity_number_When_mapped_Then_level_is_the_matching_band`

Covers COS 2. `[Theory]` rows: 1 and 4 Trace, 5 and 8 Debug, 9 and 12 Info,
13 and 16 Warning, 17 and 20 Error, 21 and 24 Critical. `severityText` is
`FATAL` on every row so a number-wins bug fails.

#### 2. `Given_severity_number_unset_When_severity_text_parsed_Then_level_follows_text`

Covers COS 2. Rows: `ERROR` → Error, `warn` → Warning, `information` → Info,
`fatal` → Critical, `nope` → Info, missing text → Info. `severityNumber` is
0. One extra case: number 9 plus text `error` → Info.

#### 3. `Given_resource_without_service_name_When_mapped_Then_source_is_unknown_service`

Covers COS 2.

- **Given** a record whose resource has `deployment.environment=prod` and no
  `service.name`
- **When** it is mapped
- **Then** `Source` is `unknown_service`
- **And** properties contain `resource.deployment.environment=prod`
- **And** properties do not contain `service.name` or
  `resource.service.name`

#### 4. `Given_non_string_body_When_mapped_Then_message_is_a_string`

Covers COS 2. Rows:

- bool `true` → `true`
- int `42` → `42`
- int string `"42"` → `42`
- double `1.5` → invariant `G17`
- bytes for ASCII `hi` → base64 `aGk=`
- array of one string `a` → compact JSON array
- kvlist `k`/`v` → compact JSON object

#### 5. `Given_trace_and_span_ids_When_mapped_Then_properties_are_lowercase_hex`

Covers COS 2.

- **Given** `traceId` `4BF92F3577B34DA6A3CE929D0E0E4736` and `spanId`
  `00F067AA0BA902B7`
- **When** mapped
- **Then** `trace_id` is `4bf92f3577b34da6a3ce929d0e0e4736` and `span_id` is
  `00f067aa0ba902b7`
- **And** a second case with trace id `not-hex` omits `trace_id` and still
  returns an entry

#### 6. `Given_time_unix_nano_string_When_mapped_Then_timestamp_uses_it`

Covers COS 2.

- **Given** `timeUnixNano` `"1710000000000000000"` and a different
  `observedTimeUnixNano`
- **When** mapped
- **Then** the timestamp is that event time, not observed time and not
  `receivedAt`
- **And** when `timeUnixNano` is `"0"` and observed is unset, the timestamp
  is `receivedAt`

#### 7. `Given_more_properties_than_the_cap_When_mapped_Then_synthetics_win_and_values_truncate`

Covers COS 2.

- **Given** a trace id, a scope name, a record attribute whose value is
  longer than 4096, a record attribute whose key is longer than 256, and
  enough further attributes to exceed 64
- **When** mapped with the default caps
- **Then** `trace_id` and `scope.name` are present
- **And** the long value is exactly 4096 characters
- **And** the long key is absent
- **And** the property count is 64

#### 8. `Given_whitespace_body_When_mapped_Then_record_is_rejected`

Covers COS 6.

- **Given** a body that is `"   "`
- **When** mapped
- **Then** the result is a rejection, not a `LogEntry`

### API

Use a unique message per test so history assertions are not order-dependent.
Auth headers go through `ApiTestAuth`.

#### 9. `Given_no_api_key_When_posting_otlp_json_Then_401`

Covers COS 3.

- **Given** enforcement on and no key header
- **When** the client POSTs a one-record JSON export
- **Then** the response is 401 and history does not contain the message

#### 10. `Given_invalid_api_key_When_posting_otlp_json_Then_403`

Covers COS 3.

- **Given** enforcement on and a key that fails validation
- **When** the client POSTs a one-record JSON export
- **Then** the response is 403 and nothing is stored

#### 11. `Given_cap_already_reached_When_posting_two_records_Then_402_and_nothing_stored`

Covers COS 4.

- **Given** `FreeIngestPerMonth` is 2, two records are already stored, and
  the export contains two valid records
- **When** the client POSTs it
- **Then** the response is 402, `error` contains `monthly ingest limit exceeded`,
  and neither message is in history

#### 12. `Given_one_slot_left_When_posting_two_records_Then_402_and_nothing_stored`

Covers COS 4. This is the straddling-batch decision.

- **Given** the cap is 2 and one record is already stored
- **When** the client POSTs two valid records
- **Then** the response is 402 and neither new message is stored

#### 13. `Given_gzip_body_When_posting_otlp_json_Then_record_is_ingested`

Covers COS 5 and 7.

- **Given** a valid one-record document compressed with gzip and
  `Content-Encoding: gzip`
- **When** the client POSTs it
- **Then** the response is 200 and history returns that message under
  `service.name`

#### 14. `Given_body_over_max_bytes_When_posting_otlp_json_Then_413`

Covers COS 5.

- **Given** `MaxRequestBodyBytes` is 1024
- **When** the client POSTs 1025 bytes of JSON
- **Then** the response is 413 and nothing is stored

#### 15. `Given_gzip_bomb_under_wire_cap_When_posting_otlp_json_Then_413`

Covers COS 5.

- **Given** `MaxDecompressedBodyBytes` is 4096 and a gzip payload smaller
  than the wire cap whose inflated size exceeds 4096
- **When** the client POSTs it
- **Then** the response is 413 and nothing is stored

#### 16. `Given_malformed_json_When_posting_otlp_Then_400`

Covers COS 5.

- **Given** the body `{`
- **When** the client POSTs it with `application/json`
- **Then** the response is 400, `error` is `malformed OTLP JSON`, and
  nothing is stored

#### 17. `Given_one_empty_body_When_posting_two_records_Then_partial_success`

Covers COS 6 and 7.

- **Given** two records, one with `stringValue` `"kept"` and one with
  `stringValue` `"  "`
- **When** the client POSTs them
- **Then** the response is 200, `partialSuccess.rejectedLogRecords` is 1,
  `errorMessage` contains `empty body`
- **And** history contains `kept` and does not contain a whitespace message
- **And** the hub received `LogReceived` once, on the group `/v1/logs` would
  use for that tenant and source

#### 18. `Given_protobuf_content_type_When_posting_Then_415`

Covers the stage boundary.

- **Given** `Content-Type: application/x-protobuf`
- **When** the client POSTs any body
- **Then** the response is 415

#### 19. `Given_two_service_names_When_posting_otlp_json_Then_each_source_has_its_line`

Covers COS 7.

- **Given** one export with `service.name` `api` and `worker`
- **When** the client POSTs it
- **Then** `GET /v1/sources/api/logs` contains only the api message and
  `GET /v1/sources/worker/logs` contains only the worker message

COS 1 is the existing `/v1/logs` suite plus one usage-unit assertion, in
`tests/QikLog.Infrastructure.Tests`:

#### 20. `Given_one_slot_remaining_When_checking_two_records_Then_denied_and_one_record_allowed`

- **Given** a limit of 2 and a current count of 1
- **When** the check runs for 2 and for 1
- **Then** 2 is denied and 1 is allowed

COS 8 reuses the stage 1 secret scan on one OTLP request when the in-memory
exporter is attached: message text and API key are absent from every span.
COS 9 is `make www-build`.
