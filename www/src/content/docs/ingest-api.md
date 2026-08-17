---
title: Ingest API
description: POST /v1/logs — JSON shape, log levels, and response codes.
order: 3
---

Send logs to the API with a single JSON endpoint.

## Endpoint

```http
POST /v1/logs
Content-Type: application/json
```

**Base URL (local):** `http://localhost:5080`  
**Base URL (cloud):** your deployed API host (e.g. `https://api.qiklog.com`)

When API keys are required, add authentication — see [API keys](/docs/api-keys/).

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

### curl example

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"payment captured"}'
```

## Responses

| Status | Meaning |
|--------|---------|
| **202 Accepted** | Log accepted, broadcast to live subscribers, queued for storage |
| **400 Bad Request** | Missing `source`/`message`, invalid JSON, or unknown `level` |
| **401 Unauthorized** | Missing or invalid API key (when auth is enabled) |
| **429 Too Many Requests** | Per-key rate limit exceeded (default 120/minute) |

## What happens after ingest

1. The API validates your payload.
2. If auth is enabled, your API key is verified and rate-limited.
3. The entry is saved to Postgres (`log_entries`).
4. SignalR pushes the line to everyone viewing `tail/{source}` in the dashboard.

## Health check

```bash
curl http://localhost:5080/healthz
```

Returns `status` and `postgres` connectivity when a database is configured.

## Next steps

- [Live tail](/docs/live-tail/) — watch lines stream in the browser
- [API keys](/docs/api-keys/) — secure ingest in production
- [Serilog](/docs/serilog/) — one-line .NET sink
