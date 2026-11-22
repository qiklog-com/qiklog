---
title: Quickstart
description: See your first live log line in under a minute, then move to your own account and API key.
order: 3
---

Nothing to install. You need a terminal and a browser.

## 1. Get a log link

```bash
curl -X POST https://qiklog.com/l
```

Copy the `url` from the response, or click **Get a log link** on [qiklog.com](https://www.qiklog.com/).

## 2. Send a line

```bash
curl https://qiklog.com/l/k7mq2x9d4fab -d "it broke"
```

A successful send returns **HTTP 202 Accepted**.

## 3. Watch it live

Open the link in your browser. Your line is there, and new ones appear as you send them.

That is the whole product. A log link lasts 24 hours, holds 500 lines, and anyone with the address can read it. See [Log links](/docs/log-links/) for levels, piping, and plain-text reads.

## 4. When you want it private and permanent

1. [Create a free account](https://app.qiklog.com/signup). You get your own workspace.
2. Open **Manage** and create an API key. Save it: it is shown once.
3. Send with the key, to a source name you choose:

```bash
export QIKLOG_API_KEY=ql_...
curl -X POST https://api.qiklog.com/v1/logs \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $QIKLOG_API_KEY" \
  -d '{"source":"api-prod","level":"info","message":"hello from curl"}'
```

4. Watch it at `https://app.qiklog.com/tail/api-prod`. Only you can read it.

Free accounts take 10,000 log lines a month. Pro takes 500,000. When you reach the cap, sending stops; reading always works.

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `404` on a log link | Check the address. Ids are 12 lowercase characters. |
| `410` on a log link | It expired after 24 hours. Get a new one. |
| `402` on a log link | It reached 500 lines. Reading still works; get a new link or use an account. |
| `401` on `/v1/logs` | The key is missing. Send `Authorization: Bearer $QIKLOG_API_KEY`. |
| Tail page empty | Confirm the `source` you sent matches the one in the tail address. |

## Next steps

- [Log links](/docs/log-links/): everything a link can do
- [Ingest API](/docs/ingest-api/): full request format
- [API keys](/docs/api-keys/): create and revoke keys
- [Serilog](/docs/serilog/): one line in Program.cs
- [Self-hosting](/docs/self-hosting/): run the stack yourself with Docker
