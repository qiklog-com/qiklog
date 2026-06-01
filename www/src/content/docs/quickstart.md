---
title: Quickstart
description: Run QikLog locally with Docker and see your first live log line.
order: 2
---

Get the full stack running on your machine in a few minutes.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) with Compose
- A browser

## 1. Start the stack

From the QikLog repository root:

```bash
docker compose up -d
```

Or use the Makefile:

```bash
make up-d
```

Wait until Postgres, Redis, API, and Web are up. Check API health:

```bash
curl http://localhost:5080/healthz
```

You should see `{"status":"ok","postgres":"ok"}` when the database is connected.

## 2. Send a test log

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

A successful ingest returns **HTTP 202 Accepted** with an empty body.

## 3. Watch it live

Open the tail page for source `demo`:

**http://localhost:5081/tail/demo**

You should see your line appear within a second. Send another curl with a different message to confirm streaming.

## 4. Verify persistence (optional)

Logs are written to the `log_entries` table in Postgres. If you have `psql` handy:

```bash
docker compose exec postgres psql -U qiklog -d qiklog -c \
  "SELECT source, level, message, received_at FROM log_entries ORDER BY received_at DESC LIMIT 5;"
```

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Connection refused on 5080 | Run `docker compose ps` — ensure `api` is running |
| Tail page empty | Confirm `source` in curl matches the URL (`demo` in `/tail/demo`) |
| 401 on ingest | You may have created API keys and enabled required auth — see [API keys](/docs/api-keys/) |
| Postgres not ok in healthz | Wait for Postgres healthcheck; run `docker compose logs postgres` |

## Next steps

- [Ingest API](/docs/ingest-api/) — full request format
- [API keys](/docs/api-keys/) — secure production ingest
