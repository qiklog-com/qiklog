# HOMER.md — Status Log for Product Owner

## Current State
- Branch: `main` (synced with `origin/main` after push)
- Latest tag: `v0.9.1-signalr-auth`
- Working state: **green**
- Test count and pass rate: **72/72 passing** (24 Core + 43 Api + 5 Infrastructure; `Category!=E2E`)
- Coverage: **~45.1%** Api-test cobertura line-rate (was 43.7% at v0.9.0; target 60% by Tier 2 complete)
- E2E last verified: not run this session (`make test` only; docker compose smoke not re-run)
- Last commit: Tier 2B — SignalR auth + `docs/QUICKSTART.md` — tag `v0.9.1-signalr-auth`
- GitHub: https://github.com/qiklog-com/qiklog

## Last Session Summary
**Date:** 2026-06-01  
**Prompt received from PO:** Tier 2B — SignalR hub auth, quickstart docs, `/v1/dev/keys` Production guard verification.  
**Work completed:**
- Locked `/hubs/logs`: JWT or `X-QikLog-API-Key`; connection refused without valid tenant credentials
- Tenant-scoped SignalR groups (`tenant:{id}:source:{name}`) when enforcement on; ingest broadcast uses same groups
- Shared `TenantAuthenticationService` (middleware + hub); JwtBearer `access_token` query for hub WebSockets
- Web tail: optional `QikLog:HubApiKey` for authenticated hub subscribe in full-dev mode
- `docs/QUICKSTART.md` — Mode A (demo, auth off) and Mode B (Zitadel via compose overlay)
- Docker compose: demo defaults disable auth; `docker-compose.auth.yml` enables enforcement
- **8 new Api tests** (`SignalRHubAuthTests` + `DevKeysEndpointTests`); load test uses one connection × 100 group subscriptions (stable under TestServer)
- `/v1/dev/keys` remains `IsDevelopment()` only; Production factory test expects **404**
**Decisions made (and why):**
- Hub auth on negotiate (middleware) + `OnConnectedAsync` fallback when WebSocket context lacks negotiate items
- Load test: 100 group subscriptions on one authenticated connection (avoids TestServer flakiness with 100 TCP connections)
**Issues encountered:**
- Production `WebApplicationFactory` for dev-keys test needs in-memory DbContext + tenant DI stubs (no Postgres in CI)
- Blended coverage across three cobertura files is misleading; report Api-test file line-rate (~45%) for continuity with 2A
**Files changed:** Api auth/hub/middleware, Web tail, compose, `docs/QUICKSTART.md`, `README.md`, `SignalRHubAuthTests.cs`, `SignalRLoadTests.cs`, `HOMER.md`

## Open Questions for PO
_(Tier 2B questions resolved — see Session History.)_

## Suggested Next Steps
1. **Persistence hardening (Redis #16)** — hot buffer + SignalR backplane before multi-instance deploy
2. **Azure deploy** — wire Zitadel/OIDC env vars; confirm hub `access_token` / API key paths in Container Apps ingress
3. **E2E smoke** — Playwright or scripted compose verify for QUICKSTART Mode A (not run locally this session)

## Session History

### 2026-06-01 — Tier 2B SignalR auth + quickstart (`v0.9.1-signalr-auth`)
`/hubs/logs` requires JWT or API key; tenant-isolated groups; QUICKSTART demo/full modes. 72/72 tests. ~45% Api coverage line-rate.

### 2026-06-01 — Project Gate protocol adopted. Signatures: 🖖 (Garfield), 🍩 (Homer).
Homer prompts include a PROJECT GATE block naming expected repo; Garfield stops on mismatch before executing.

### 2026-06-01 — Tier 2A auth enforcement (`v0.9.0-auth-enforcement`)
Mandatory tenant context on management (OIDC JWT), ingest (`X-QikLog-API-Key`), history (JWT or API key). 64/64 tests. Coverage 43.7% blended.

### 2026-06-01 — GitHub sync + HOMER verification checklist
Confirmed hardening on `origin/main`; stopped stale dev processes.

### 2026-06-01 — PO/Dev protocol + HOMER.md
Established HOMER.md. Repo at `v0.8.1-hardening`, 53/53 tests.

### 2026-06-01 — Tier 1.5 hardening (`v0.8.1-hardening`)
OpenAPI, observability, tenant isolation tests, FsCheck billing math.

### 2026-06-01 — Tier 1 launch track (`v0.3.0` … `v0.8.0`)
Management API, history, OIDC, Stripe, www legal, doc automation.
