# HOMER.md — Status Log for Product Owner

## Current State
- Branch: `main` (synced with `origin/main`)
- Latest tag: `v0.9.0-auth-enforcement`
- Working state: **green**
- Test count and pass rate: **64/64 passing** (24 Core + 35 Api + 5 Infrastructure; `Category!=E2E`)
- Coverage: **43.7% blended** (was ~43% pre-auth; target 60% by Tier 2 complete)
- E2E last verified: not run this session (`make test` only)
- Last commit hash and date: `4730ffb` — 2026-06-01 — `feat(auth): enforce tenant-scoped JWT and API key auth on all protected routes`
- GitHub: https://github.com/qiklog-com/qiklog

## Last Session Summary
**Date:** 2026-06-01  
**Prompt received from PO:** Adopt Project Gate protocol — verify repo before executing Homer prompts; document in HOMER.md.  
**Work completed:**
- Recorded Project Gate convention in Session History (🖖 Garfield / 🍩 Homer)
- Developer will check gate block first; stop with "Wrong window" if repo mismatch
**Decisions made (and why):**
- Documentation-only change; no code impact
**Issues encountered:** None.  
**Files changed:** `HOMER.md`

## PO protocol answers (recorded)
1. **Session boundaries:** Update HOMER.md after any committed+pushed unit of work between Homer prompts.
2. **E2E:** `make test` gates green; optional `E2E last verified` field in Current State.
3. **Branches:** Stay on `main`; direct commits; tags + commits are audit trail.
4. **Coverage:** Track blended % each session; target 60% by Tier 2 complete; no hard minimum yet.

## Open Questions for PO
1. **SignalR hub `/hubs/logs`:** Still unauthenticated (not in Tier 2A scope). Lock down in 2B?
2. **Local dev without Zitadel:** Management/ingest require auth when Postgres + `AuthEnforcement` enabled; need `docker compose` + OIDC or Testing host. Document in quickstart?
3. **`/v1/dev/keys`:** Now requires JWT like management — keep for Development only?

## Suggested Next Steps
1. **Wire API JWT from Zitadel in docker-compose** — enable `QikLog:Auth:Enabled` on API with real tokens (dashboard → API calls).
2. **SignalR auth** — subscribe requires tenant-scoped credential.
3. **Persistence hardening (Redis #16)** or **Azure deploy** — auth work surfaced need for configured OIDC in deployed environments.

## Session History

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
