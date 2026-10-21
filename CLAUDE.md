# QikLog

Lightweight log tailing for .NET developers: `tail -f` in a browser, plus a CLI and SDKs. Pre-alpha, invite-only beta live. Goal now: a product strangers can sign up for and pay for.

## Shared rules (also used by Cursor)

@.cursor/rules/qiklog.mdc
@.cursor/rules/user-docs.mdc
@docs/TESTING.md

Note: the "What we are NOT building right now" list in qiklog.mdc predates Phase 2. Auth, multi-tenancy, and Postgres persistence now exist. Current scope is below; it wins when the two disagree.

## Current scope

1. **Milestone 1, self-serve:** a stranger can sign up, create a source and API key, and see their own logs with no manual database work (Path B in POST_LAUNCH.md). Serilog sink batches and retries instead of silently dropping. New package `QikLog.Extensions.Logging` (an `ILoggerProvider`).
2. **Azure move:** QikLog goes back to Azure Container Apps from Railway (see docs/AZURE_DEPLOY.md). Railway stays live until DNS cutover is approved.
3. Later: Stripe checkout, search, retention per plan, alerts.

Out of scope until asked: mobile/desktop clients in `clients/`, new brand work, OpenTelemetry.

## Layout

- `src/QikLog.Core` domain, `QikLog.Api` minimal API + SignalR `LogHub`, `QikLog.Web` Blazor Server dashboard, `QikLog.Cli`, `QikLog.Infrastructure` (Postgres), `QikLog.Serilog` sink (on NuGet).
- `tests/` xUnit + Shouldly. `QikLog.Smoke.Tests` hit a live deployment and only run with `QIKLOG_SMOKE=1`.
- `www/` Astro marketing site and end-user docs (Vercel).
- `scripts/` + `Makefile` deploy and dev tooling. `docs/` internal docs.

## Commands

- `make test` before every commit. CI runs the same filter (`Category!=E2E&Category!=Smoke`).
- `make up-d` / `make verify` local stack; `make smoke` against production (URLs in Makefile).
- `make azure-setup`, `make azure-deploy` for Azure.

## Replicants

Work is split between Bob replicants defined in `.claude/agents/`:

- **garfield** — application code: `src/`, `tests/`, `www/`.
- **bill** — infrastructure: `infra/`, `scripts/`, `.github/workflows/`, Dockerfiles, deploy docs.
- **reviewer** — read-only PR review.

Each replicant stays in its lane, works on its own branch (ideally its own git worktree), and keeps notes in its project memory under `.claude/agent-memory/<name>/`. Homer is not a QikLog replicant; HOMER.md is historical status from earlier sessions. Read it for context, but don't write to it.

## Always

- Never commit secrets or print them. `.env`, `appsettings.Development.json`, `secrets.json` stay local.
- Conventional Commits, small commits, PRs to `main`. Never push to `main` directly.
- Ask Jamey before: DNS changes, deleting cloud resources or data, anything that adds more than $20/month, publishing a NuGet package, or sending email.
