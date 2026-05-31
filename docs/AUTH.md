# Authentication & Identity

## Decision

**QikLog uses Zitadel as the identity provider, self-hosted at `signin.qiklog.com`.**

Applications (Blazor Web, API, CLI, future products) integrate via OpenID Connect (OIDC). No identity code lives inside QikLog itself.

## Why Zitadel

1. **Built for multi-tenancy.** Native hierarchy: Instance → Organization → Project → Application. Each QikLog tenant is a Zitadel Organization; same model extends to future products.
2. **Microservice-shaped.** OIDC + gRPC APIs. Any future Jamey project (Christ Medical, jameymcelveen.com, the next thing) can point at the same `signin.qiklog.com` and get free SSO.
3. **Feature complete.** Username/password, social (Google/GitHub/Microsoft), passkeys, TOTP, magic links, SMS, SAML, SCIM — all native, no plugin gymnastics.
4. **Operationally simple.** Single Go binary + PostgreSQL. Runs in Docker locally, in Azure Container Apps in production. ~100 MB RAM footprint.
5. **Standards-based exit.** If Zitadel ever fails us, OIDC is portable. Swap providers without changing application code.

## Why not the alternatives

| Option | Why we passed |
|--------|---------------|
| **ASP.NET Core Identity + OpenIddict/Duende** | 200-400 hours to build to feature parity. Identity is opportunity cost theater for an indie project. Duende is now ~$1500/year. |
| **Auth0 / Clerk / Stytch (hosted)** | Excellent products. Skipped because (a) self-hosted teaches transferable skills, (b) data sovereignty matters for future regulated-industry customers, (c) cost scales painfully past free tier. Reconsider if Zitadel ops become a burden. |
| **Keycloak** | JVM weight, Java-flavored configuration model, multi-tenancy is bolted on rather than native. Better for large enterprises than for indie SaaS. |
| **Authentik** | Strong product. Multi-tenancy is secondary; Python/Django stack is further from our wheelhouse. |

## Phasing (replaces previous "ASP.NET Core Identity" tickets in PROJECT_PLAN.md)

### Phase 2 — Auth foundation

- [ ] **#A1** Add Zitadel to `docker-compose.yml` (local dev only)
- [ ] **#A2** Configure first Zitadel instance: realm, organization, project, web application
- [ ] **#A3** Wire `QikLog.Web` to Zitadel via `Microsoft.AspNetCore.Authentication.OpenIdConnect`. `[Authorize]` works, login/logout buttons work.
- [ ] **#A4** Enable username/password + email magic link auth methods
- [ ] **#A5** Add Google, GitHub, Microsoft as external identity providers in Zitadel
- [ ] **#A6** Wire `QikLog.Api` to validate Zitadel-issued access tokens (`AddJwtBearer` with Zitadel as authority)
- [ ] **#A7** Map QikLog's `Tenant` table to Zitadel `Organization` IDs (one-to-one)
- [ ] **#A8** EF Core global query filters: every tenant-scoped query filters by current user's org claim

### Phase 2.5 — MFA and passkeys

- [ ] **#A9** Enable passkey (FIDO2/WebAuthn) auth in Zitadel
- [ ] **#A10** Enable TOTP 2FA in Zitadel
- [ ] **#A11** Recovery codes flow

### Phase 3 — CLI auth

- [ ] **#A12** Implement OAuth 2.0 device authorization flow in `QikLog.Cli`
- [ ] **#A13** `qiklog login` command — prints code + URL, polls for token, stores in OS keychain (DPAPI on Windows, Keychain on macOS, libsecret on Linux)
- [ ] **#A14** `qiklog logout`, `qiklog whoami` commands
- [ ] **#A15** CLI auto-refreshes tokens before they expire

### Phase 4 — Production

- [ ] **#A16** Deploy Zitadel to Azure as `signin.qiklog.com` (Container App + Postgres Flexible Server)
- [ ] **#A17** Configure custom branding (QikLog logo, colors per BRAND_GUIDE.md)
- [ ] **#A18** Backup strategy for Zitadel's Postgres (daily automated, 30-day retention)
- [ ] **#A19** Secrets in Azure Key Vault; rotation runbook

### Deferred until a paying customer asks

- SAML (enterprise SSO) — Zitadel supports it; flip a switch when needed
- SCIM provisioning — same
- SMS 2FA — poor cost/security ratio; passkeys + TOTP cover the gap
- LDAP / Active Directory federation

## Multi-tenancy model

**Single-database, tenant-scoped queries.** Every tenant-scoped table has a `TenantId` column. EF Core global query filters enforce isolation automatically:

```csharp
modelBuilder.Entity<LogEntry>()
    .HasQueryFilter(e => e.TenantId == _currentTenantAccessor.TenantId);
```

`_currentTenantAccessor.TenantId` is hydrated from the OIDC access token's organization claim at the start of every request.

**Why not database-per-tenant:** strongest isolation but operationally painful (migrations × N, backup × N). Justified only for highly regulated customers at scale. Revisit if/when a healthcare or financial customer demands it.

## Token strategy

- **ID token** — used by Blazor to populate `ClaimsPrincipal`. Short-lived (1 hour).
- **Access token** — used by Blazor → API and CLI → API calls. JWT, validated locally by API via Zitadel's JWKS. Short-lived (1 hour).
- **Refresh token** — long-lived (30 days), used to obtain new access tokens silently. Stored encrypted server-side for web sessions; in OS keychain for CLI.

API validates access tokens with `AddJwtBearer`, configuring `Authority = "https://signin.qiklog.com"` and `Audience = "qiklog-api"`. No shared secrets between API and Web.

## Local dev setup

`docker-compose.yml` includes a Zitadel service alongside Postgres/Redis/API/Web. First-run initialization seeds:
- Instance admin: `zadmin@qiklog.local` (password printed to logs on first start)
- One organization: "QikLog Dev"
- One project: "QikLog"
- One application: "QikLog Web" (OIDC, code flow + PKCE)
- One test user: `dev@qiklog.local` / `Password1!`

The Blazor app is configured against `http://localhost:8080` (the Zitadel container). In production, this becomes `https://signin.qiklog.com`.

## Operational notes

- **Zitadel version pinning:** pin to a specific minor version in docker-compose and the Azure deployment. Don't track `:latest`. Auth providers are not the place for surprise upgrades.
- **License awareness:** Zitadel is AGPL-3.0 as of 2025. We consume it as an external OIDC provider — no source modification — so AGPL doesn't restrict us. If we ever wanted to embed Zitadel code into QikLog itself, AGPL would require us to publish modifications. Not a current concern.
- **CVE watch:** subscribe to Zitadel's GitHub security advisories. Patch within 7 days of critical CVE publication.
