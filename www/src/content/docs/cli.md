---
title: CLI
description: qiklog send, watch, tail-file, and key management from the terminal.
order: 7
---

The `qiklog` CLI ships and receives logs from your terminal — useful for scripts, CI, live tail, and tailing local files.

## Install / run from source

From the repository (requires .NET 9 SDK), install a global `qiklog` on your PATH:

```bash
make install-cli
# → ~/.local/bin/qiklog  (override: make install-cli PREFIX=/usr/local/bin)
qiklog --help
```

Remove with `make uninstall-cli`. Or run without installing:

```bash
dotnet run --project src/QikLog.Cli -- --help
```

Homebrew / Scoop packages are planned later.

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

The CLI sends `Authorization: Bearer <key>` (same credential the API also accepts as `X-QikLog-API-Key`). There is no interactive OAuth login for `send` / `watch` / `tail-file` — that is intentional for agent-style ingest.

Exit code `0` on **202 Accepted**; non-zero on failure with stderr details.

## watch — live-tail a source

Connects to the same SignalR hub as the browser (`/hubs/logs`), calls `Subscribe` for a source, and prints each `LogReceived` line to stdout:

```bash
dotnet run --project src/QikLog.Cli -- watch \
  --source demo \
  --api https://api.qiklog.com \
  --key "$QIKLOG_API_KEY"
```

Press `Ctrl+C` to stop. Lines look like `HH:mm:ss.fff LEVEL source message`.

**No history on subscribe.** The hub joins the source group only; it does not replay the buffer. Open a watch session first, then `send` (or curl) to the same source to see lines. For past entries, use the browser tail page or `GET /v1/sources/{source}/logs`.

Wrong or missing keys fail with a clear stderr message (exit code `1`), not a hung connection.

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

**Watch live (CLI or browser)**

```bash
qiklog watch -s production --api https://api.qiklog.com --key "$QIKLOG_API_KEY"
```

Or open `/tail/production` on the web app while shipping logs.

## Next steps

You are through the core docs. Wire a .NET app with [Serilog](/docs/serilog/), revisit [Getting started](/docs/) for the mental model, or run [Quickstart](/docs/quickstart/) again on a fresh machine.
