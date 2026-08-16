# Testing convention (QikLog)

House process for non-trivial changes. Work orders can say "follow the usual
process" and mean this file.

This is BDD / ATDD discipline, not Cucumber. Scenarios live in **xUnit** with
**Shouldly**. We do not add Reqnroll / SpecFlow / Playwright unless the PO asks.
Smoke tests use `HttpClient` against a live deployment (`QIKLOG_SMOKE=1`).

## Process (do not skip or reorder)

### 1. Define the task

State in your own words: what is broken or missing, why, and what "fixed" means.
Confirm you understand the code you will touch before editing it.

### 2. Conditions of Satisfaction (COS)

Numbered list of what "done" means. Each item must be testable and specific.
No vague "works better." Show the list before writing scenario code when the
work order asks for it, or when COS is ambiguous.

### 3. Executable scenarios (Given / When / Then)

Write these **before** the fix so they fail first, then pass.

- Naming: `Given_X_When_Y_Then_Z`
- Method body starts with `// Given:` / `// When:` / `// Then:` comments
- Place them in:
  - `tests/QikLog.Smoke.Tests` for end-to-end, auth, API boundary, live deploy
  - the relevant `*.Tests` project for internal behavior

Opt-in smoke: use `[SmokeFact]`, `[AuthenticatedSmokeFact]`, or
`[OidcSmokeFact]` so offline CI skips visibly. Do **not** silent-return and
look like a pass.

### 4. Unit tests under the acceptance layer

Fine-grained net in `QikLog.Api.Tests`, `QikLog.Core.Tests`,
`QikLog.Infrastructure.Tests`, etc. Cover the actual logic: happy path plus at
least one failure case. xUnit + Shouldly. File-scoped namespaces. `sealed` by
default.

### 5. Validate

Run the **full** offline suite (`make test`), not only new tests. Report:

- COS → which test covers it
- pass/fail counts **before** and **after** the change

Smoke against production: `make smoke` (and set `QIKLOG_SMOKE_ACCESS_TOKEN` /
`QIKLOG_SMOKE_API_KEY` when the scenario needs them).

## Example (JWT audience, condensed)

**Task:** API rejects web OIDC tokens because audience is `"qiklog-api"` while
Zitadel issues the project id.

**COS:**

1. Signed-in user can call an authenticated API route and get 200, not 401.
2. Audience validation stays strict (no bypass / wildcard).
3. Ingest API-key path unchanged.
4. Existing auth tests still pass.

**Scenario (smoke):**

```csharp
[OidcSmokeFact]
public async Task Given_signed_in_user_When_calling_manage_endpoint_Then_returns_200()
{
    // Given: a user has completed OIDC sign-in (access token in env)
    // When: they GET /v1/sources with Bearer
    // Then: 200, not 401 or 403
}
```

**Unit:** correct audience accepted; wrong / missing / malformed rejected.

## Where tests live

| Concern | Project | How it runs |
| --- | --- | --- |
| Domain / contracts / pure logic | `QikLog.Core.Tests` | `make test` |
| EF / tenants / keys | `QikLog.Infrastructure.Tests` | `make test` |
| HTTP API / auth middleware | `QikLog.Api.Tests` | `make test` |
| Live deploy | `QikLog.Smoke.Tests` | `make smoke` (`Category=Smoke`) |
| Doc screenshots | `QikLog.DocGen.Tests` | `make docs-capture` (`Category=E2E`) |

CI and `make test` exclude `Category=Smoke` and `Category=E2E`.

## Smoke environment variables

| Variable | Purpose |
| --- | --- |
| `QIKLOG_SMOKE=1` | Enable live smoke |
| `QIKLOG_SMOKE_WEB_URL` | Override web origin (default Railway web) |
| `QIKLOG_SMOKE_API_URL` | Override API origin (default Railway api) |
| `QIKLOG_SMOKE_API_KEY` | Real ingest key for round-trip scenarios |
| `QIKLOG_SMOKE_ACCESS_TOKEN` | Zitadel JWT access token after sign-in (Manage path) |
| `QIKLOG_TIMING=1` | Opt-in send→SignalR latency measurement (reports ms; not a CI gate). Needs `QIKLOG_SMOKE_API_KEY`. |

## Auth regressions this process must catch

These broke in production via manual poking, not tests. Backfill coverage lives
under smoke + unit/contract tests:

1. `QikLog__Auth__Enabled=false` on the API while web is signed in
2. Zitadel access token type opaque Bearer vs JWT (audience never visible)
3. JWT audience `"qiklog-api"` vs Zitadel project id
4. Tenant org claim on cookie/userinfo vs access token (web vs API mismatch)
5. Custom domain TLS (smoke against `app.qiklog.com` / `api.qiklog.com`)

## Do not

- Add NuGet packages for testing without asking
- Weaken JWT validation to make a test green
- Touch Stripe / DNS / Railway secrets from tests
- Call silent `return;` inside smoke Facts when env is missing; skip instead
