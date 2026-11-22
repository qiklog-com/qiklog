---
title: Log links
description: One address you send a line to and open in a browser. No account, no key.
order: 2
---

A log link is `console.log` for code that is not on your machine. You get one address. You send a line to it. You open the same address to watch.

## Get a link

Click **Get a log link** on [qiklog.com](https://www.qiklog.com/), or from a terminal:

```bash
curl -X POST https://qiklog.com/l
```

```json
{
  "id": "k7mq2x9d4fab",
  "url": "https://qiklog.com/l/k7mq2x9d4fab",
  "raw": "https://qiklog.com/l/k7mq2x9d4fab/raw",
  "expiresAt": "2026-11-22T20:59:03Z",
  "maxLines": 500,
  "send": "curl https://qiklog.com/l/k7mq2x9d4fab -d \"it broke\""
}
```

## Send a line

The request body is the line. No key, no JSON, no headers.

```bash
curl https://qiklog.com/l/k7mq2x9d4fab -d "it broke"
```

```javascript
await fetch("https://qiklog.com/l/k7mq2x9d4fab", { method: "POST", body: "it broke" });
```

```csharp
await new HttpClient().PostAsync("https://qiklog.com/l/k7mq2x9d4fab", new StringContent("it broke"));
```

Pipe a program into it. Each line of output becomes a line (up to 50 per request):

```bash
./deploy.sh 2>&1 | curl https://qiklog.com/l/k7mq2x9d4fab --data-binary @-
```

### Levels

A line is `info` unless you say otherwise. Any one of these sets the level:

| How | Example |
|-----|---------|
| A leading token | `ERROR 500 POST /checkout`, `warn: slow query`, `[debug] cache miss` |
| The `level` query value | `curl "https://qiklog.com/l/k7mq2x9d4fab?level=error" -d "it broke"` |
| JSON with `message` and `level` | `-H "Content-Type: application/json" -d '{"message":"it broke","level":"error"}'` |

Levels: `trace`, `debug`, `info`, `warn`, `error`, `critical`.

## Watch it

Open the link in a browser. That is the live tail.

From a terminal or a coding agent, read it as plain text:

```bash
curl https://qiklog.com/l/k7mq2x9d4fab/raw
curl "https://qiklog.com/l/k7mq2x9d4fab/raw?last=20"
```

```text
2026-11-21T20:59:03.120Z INFO  it broke
2026-11-21T20:59:07.482Z ERROR ERROR 500 POST /checkout
```

`last` is how many lines to return, newest at the bottom (default 100, max 500).

### For coding agents

An agent that is debugging deployed code cannot open a dashboard, but it can use curl. Put this in `CLAUDE.md` or `.cursor/rules`:

```text
When debugging deployed code, do not guess. Log to QIKLOG_URL and read it back.
Send:  curl "$QIKLOG_URL" -d "checkout: cart was null"
Read:  curl "$QIKLOG_URL/raw?last=50"
```

## Limits

| Limit | Value |
|-------|-------|
| Lifetime | 24 hours |
| Lines per link | 500 |
| Characters per line | 2,048 |
| Lines per request | 50 |
| New links per address | 20 an hour |

## Status codes

| Code | Meaning |
|------|---------|
| `201 Created` | Link created |
| `202 Accepted` | Line stored and sent to live viewers |
| `400 Bad Request` | The body was empty |
| `402 Payment Required` | The link is full. Reading still works. |
| `404 Not Found` | No such link |
| `410 Gone` | The link has expired |
| `413 Payload Too Large` | A line was too long, or too many lines in one request |
| `429 Too Many Requests` | Too many new links from your address this hour |

## A link is unlisted, not private

Anyone who has the address can read the lines and send to it. The address is long and random, so it cannot be guessed, but treat it like a shared document link:

- Do not log passwords, tokens, or customer data to a log link.
- Do not leave a log link in production code.

For logs that only you can read and that do not expire, [create a free account](/docs/quickstart/) and use an API key.

## Next steps

- [Quickstart](/docs/quickstart/): from a log link to your own account
- [Ingest API](/docs/ingest-api/): the keyed endpoint, with structured properties
- [Serilog](/docs/serilog/): send your .NET logs with one line
