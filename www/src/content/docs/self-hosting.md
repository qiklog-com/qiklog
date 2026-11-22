---
title: Self-hosting
description: Run the QikLog stack on your own machine with Docker.
order: 11
---

You do not need this to use QikLog: the hosted service at qiklog.com needs no install (see [Quickstart](/docs/quickstart/)). This page is for running the whole stack yourself.

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
