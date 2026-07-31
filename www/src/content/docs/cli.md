---
title: CLI
description: qiklog send, tail-file, and key management from the terminal.
order: 6
---

The `qiklog` CLI ships logs from your terminal — useful for scripts, CI, and tailing local files.

## Install / run from source

From the repository (requires .NET 9 SDK):

```bash
dotnet run --project src/QikLog.Cli -- --help
```

A packaged single-file binary is planned for Homebrew and Scoop.

## Global options

| Option | Env var | Default | Description |
|--------|---------|---------|-------------|
| `--api` | — | `http://localhost:5080` | API base URL |
| `--key` / `-k` | `QIKLOG_API_KEY` | — | API key when auth is enabled |

## send — one log line

```bash
dotnet run --project src/QikLog.Cli -- send \
  --source demo \
  --message "deploy finished" \
  --level info
```

With an API key:

```bash
dotnet run --project src/QikLog.Cli -- send \
  -s demo -m "deploy finished" \
  --key "$QIKLOG_API_KEY"
```

Against the hosted API (invite beta):

```bash
export QIKLOG_API_KEY='ql_…'   # from Manage or an invite
dotnet run --project src/QikLog.Cli -- send \
  --api https://qiklog-api.up.railway.app \
  -s demo -m "hello from the CLI" -l info
```

The CLI sends `Authorization: Bearer <key>` (same credential the API also accepts as `X-QikLog-API-Key`). There is no interactive OAuth login for `send` / `tail-file` — that is intentional for agent-style ingest.

Exit code `0` on **202 Accepted**; non-zero on failure with stderr details.

## tail-file — ship a local file

Follows a file like `tail -f` and POSTs each new line:

```bash
dotnet run --project src/QikLog.Cli -- tail-file ./app.log --source mybox
```

Press `Ctrl+C` to stop. Useful for dev boxes and quick demos without changing application code.

## key create — development only

Creates a key via `POST /v1/dev/keys` (API must be in Development):

```bash
dotnet run --project src/QikLog.Cli -- key create --name "local dev"
```

Prints JSON with the new key. Store it in `QIKLOG_API_KEY` or your password manager.

## Typical workflows

**Smoke test after deploy**

```bash
qiklog send -s production -m "release v1.2.3 live" -l info --api https://api.qiklog.com --key "$QIKLOG_API_KEY"
```

**Watch in browser**

Open `/tail/production` on the web app while shipping logs.

## Next steps

You are through the core docs. Revisit [Getting started](/docs/) for the mental model, or run [Quickstart](/docs/quickstart/) again on a fresh machine.
