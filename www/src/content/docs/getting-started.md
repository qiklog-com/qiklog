---
title: Getting started
description: What QikLog is, core concepts, and the five-minute mental model.
order: 1
---

QikLog is real-time log tailing for developers: send a line, watch it appear in the browser. Like `tail -f`, but over HTTP and SignalR.

## Core concepts

| Term | Meaning |
|------|---------|
| **Source** | A named stream of logs (e.g. `api-prod`, `worker-01`, `demo`). You choose the name; it groups live viewers and stored rows. |
| **Level** | Severity: `trace`, `debug`, `info`, `warning`, `error`, `critical` (or integers 0–5). |
| **Log link** | One address you send to and open, with no account: `POST /l/{id}` to send, open it in a browser to watch. |
| **Ingest** | `POST /v1/logs` accepts JSON and broadcasts to anyone tailing that source. |
| **Live tail** | The dashboard page `/tail/{source}` shows new lines as they arrive. |
| **Persistence** | Ingested lines are stored in Postgres so history survives restarts (search UI coming later). |

## What you need

- **API**: receives logs (default local port `5080`)
- **Web app**: Blazor dashboard for live tail (default local port `5081`)
- **Optional CLI**: `qiklog send` and `qiklog tail-file` from your terminal

## Fastest path

1. [qiklog.com](https://www.qiklog.com/): click **Get a log link**. No account.
2. [Log links](/docs/log-links/): send a line with one `curl`, watch it in the browser
3. [Quickstart](/docs/quickstart/): from a log link to your own account and API key
4. [Ingest API](/docs/ingest-api/): `POST /v1/logs` with Bearer or `X-QikLog-API-Key`
5. [Live tail](/docs/live-tail/): open the browser viewer
6. [API keys](/docs/api-keys/): required on the hosted service; optional locally
7. [Serilog](/docs/serilog/): one line in Program.cs
8. [CLI](/docs/cli/): ship logs from scripts and files
9. [Manage UI](/docs/manage-ui/): list sources and keys in the dashboard
10. [Self-hosting](/docs/self-hosting/): run the stack yourself with Docker

Hosted ingest (key required):

```bash
curl -X POST https://api.qiklog.com/v1/logs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $QIKLOG_API_KEY" \
  -d '{"source":"demo","level":"info","message":"hello from curl"}'
```

## Beta honesty

QikLog is in beta.

- A **log link** is unlisted, not private. Anyone who has the address can read it. It lasts 24 hours and holds 500 lines. Do not log secrets to it.
- An **account** gives you private sources and API keys. Only you can read them.
- Whatever you send, do not log passwords, tokens, or card numbers.
