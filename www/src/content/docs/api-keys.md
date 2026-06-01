---
title: API keys
description: Create keys, authenticate ingest, and understand rate limits.
order: 5
---

API keys protect the **ingest** endpoint so random traffic cannot spam your project.

## When keys are required

| Environment | Default |
|-------------|---------|
| **Local Docker Compose** | Keys optional (`RequireApiKey=false`) — quickstart curl works without a key |
| **Production** | Keys **required** on every `POST /v1/logs` |

If you create keys locally and enable `RequireApiKey`, ingest without a key returns **401**.

## Key format

Keys look like:

```text
ql_{8-char-prefix}_{secret}
```

Example shape: `ql_a1b2c3d4_xYz9...` — the full secret is shown **once** at creation.

## Create a key (dashboard)

Open **Manage → API keys** at `/manage` on the web app, click **Create API key**, and save the plaintext from the dialog.

## Create a key (CLI / curl)

With the API running in **Development**:

```bash
dotnet run --project src/QikLog.Cli -- key create --name "my laptop"
```

Or call the dev endpoint directly:

```bash
curl -X POST http://localhost:5080/v1/dev/keys \
  -H "Content-Type: application/json" \
  -d '{"name":"ci pipeline"}'
```

The response includes the plaintext `key`. **Save it now** — it cannot be retrieved later.

## Use a key on ingest

### Authorization header (recommended)

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "Authorization: Bearer ql_your_full_key_here" \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","level":"info","message":"authenticated"}'
```

### X-Api-Key header

```bash
curl -X POST http://localhost:5080/v1/logs \
  -H "X-Api-Key: ql_your_full_key_here" \
  -H "Content-Type: application/json" \
  -d '{"source":"demo","message":"hello"}'
```

### Environment variable for CLI

```bash
export QIKLOG_API_KEY=ql_your_full_key_here
dotnet run --project src/QikLog.Cli -- send -s demo -m "hello"
```

## Security notes

- Keys are stored as **Argon2id** hashes; only a lookup prefix is stored in plain text.
- Revocation UI is coming with source management (#13). Until then, deactivate keys in the database or rotate by creating a new key.
- Never commit keys to git or paste them into public tickets.

## Rate limits

Each key is limited to **120 ingest requests per minute** by default. Over the limit you receive **429 Too Many Requests**.

## Next steps

- [CLI](/docs/cli/) — `send` and `tail-file` with `--key`
- [Ingest API](/docs/ingest-api/) — request format reference
