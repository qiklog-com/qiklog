---
title: Ingest API
description: POST /v1/logs JSON shape, auth headers, log levels, and response codes.
order: 3
---

Send logs to the API with a single JSON POST. Same contract for curl, the CLI, and the Serilog sink.

## Endpoint

```http
POST /v1/logs
Content-Type: application/json
```

| Environment | Base URL |
|-------------|----------|
| Local Docker | `http://localhost:5080` |
| Hosted | `https://api.qiklog.com` |

## Authentication

Production requires a key on every ingest. Local Docker defaults to keys optional. See [API keys](/docs/api-keys/).

Send the key with **one** of these headers:

| Header | Example |
|--------|---------|
| `Authorization: Bearer` | `Authorization: Bearer ql_your_full_key_here` (recommended) |
| `X-QikLog-API-Key` | `X-QikLog-API-Key: ql_your_full_key_here` |
| `X-Api-Key` | `X-Api-Key: ql_your_full_key_here` (legacy alias) |

CLI and Serilog send `Authorization: Bearer`. The landing tape uses the same hosted curl.

## Hosted curl

```bash
export QIKLOG_API_KEY='ql_your_full_key_here'

curl -X POST https://api.qiklog.com/v1/logs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $QIKLOG_API_KEY" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

Use source `demo` to see the line on [the live landing panel](https://www.qiklog.com/) and at `/tail/demo` on the dashboard.

Equivalent with the product header:

```bash
curl -X POST https://api.qiklog.com/v1/logs \
  -H "Content-Type: application/json" \
  -H "X-QikLog-API-Key: $QIKLOG_API_KEY" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

A successful ingest returns **HTTP 202 Accepted** with an empty body.

## Local curl

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"payment captured"}'
```

When local auth is on, add the same Bearer or `X-QikLog-API-Key` header as production.

## Request body

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `source` | Yes | string | Logical source name (trimmed). Use stable names per app or environment. |
| `message` | Yes | string | Log line body (plain text). |
| `level` | No | string or int | Default `info`. See levels below. |
| `timestamp` | No | ISO 8601 | Event time (UTC). Defaults to server receipt time. |
| `properties` | No | object | Flat key/value strings for structured metadata. |

### Example

```json
{
  "source": "checkout-api",
  "level": "warning",
  "message": "payment retry attempt 2",
  "timestamp": "2026-06-01T12:00:00Z",
  "properties": {
    "orderId": "ord_123",
    "region": "us-east"
  }
}
```

### Log levels

Accepted as **case-insensitive strings** or **integers**:

| String | Int | Use for |
|--------|-----|---------|
| `trace` | 0 | Verbose tracing |
| `debug` | 1 | Debug detail |
| `info` | 2 | Normal operations |
| `warning` or `warn` | 3 | Recoverable issues |
| `error` or `err` | 4 | Failures |
| `critical` or `crit` | 5 | Severe / page-worthy |

## Responses

| Status | Meaning |
|--------|---------|
| **202 Accepted** | Log accepted, broadcast to live subscribers, queued for storage |
| **400 Bad Request** | Missing `source`/`message`, invalid JSON, or unknown `level` |
| **401 Unauthorized** | Missing API key (when auth is enabled) |
| **403 Forbidden** | Invalid or revoked API key |
| **429 Too Many Requests** | Per-key rate limit exceeded (default 120/minute) |

## What happens after ingest

1. The API validates your payload.
2. If auth is enabled, your API key is verified and rate-limited.
3. The entry is saved to Postgres (`log_entries`).
4. SignalR pushes the line to everyone viewing `tail/{source}` in the dashboard.

## Health check

```bash
curl https://api.qiklog.com/healthz
```

Local: `curl http://localhost:5080/healthz`.

Returns `status` and `postgres` connectivity when a database is configured.

## Next steps

- [Live tail](/docs/live-tail/): watch lines stream in the browser
- [API keys](/docs/api-keys/): mint and send a Bearer key
- [Serilog](/docs/serilog/): one-line .NET sink
- [CLI](/docs/cli/): `qiklog send` from a terminal
