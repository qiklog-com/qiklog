# Launch checklist

From `POST_LAUNCH.md` (Garfield 2026-08-01).  
**Path A (interview link) is done.** Work Path B in order; do not start Path C early.

---

## Path A — interview / carnival (done)

- [x] Public HTTPS web + API on Railway
- [x] Auth enforcement on; anon ingest 401
- [x] CLI ships with `QIKLOG_API_KEY`
- [x] Live tail under bootstrap tenant
- [x] Intentional home + demo banner
- [x] 72/72 offline tests green
- [x] Stripe stays TEST

---

## Path B — soft launch (friends self-serve)

### 1. OIDC on for real ← **in progress** (code done; console + env flip remain)

**Code shipped (this pass):**

- [x] First login **claims** bootstrap tenant (`ZitadelOrgId` null → bind org) so hub keys are not orphaned
- [x] Web forwards OIDC access token to API (Manage + history); hub prefers Bearer, falls back to `HubApiKey`
- [x] OIDC `MapInboundClaims=false`, PKCE, secure cookies, `/logout`, Sign out link
- [x] Railway API: `QikLog__Auth__ApiAudience=qiklog-api` set
- [x] Unit tests for tenant claim / provision

**You (Zitadel console + Railway secrets) — required before flipping Enabled:**

- [ ] Zitadel application: Access Token Type = **JWT** (not Bearer opaque)
- [ ] Zitadel project: API / audience includes resource checked by API (`qiklog-api` or the project id Zitadel puts in `aud` — if tokens use a numeric project id, set `QikLog__Auth__ApiAudience` to that value on the API)
- [ ] Redirect URI allowlist: exact `https://qiklog.up.railway.app/signin-oidc` (no trailing slash)
- [ ] Post-logout URI: `https://qiklog.up.railway.app/`
- [ ] Railway **Web**: set `QikLog__Auth__ClientSecret` (missing today — confidential app will fail token exchange without it). Or convert the Zitadel app to **PKCE public** and leave secret empty.
- [ ] Railway **Web**: `QikLog__Auth__Enabled=true` (Authority + ClientId already set)
- [ ] Railway **API**: `QikLog__Auth__Enabled=true` (Authority + Management already set; ApiAudience set)
- [ ] Smoke: Sign in → Manage → create key → CLI send → `/tail/{source}` for that tenant

**Do not enable Auth until ClientSecret (or public PKCE) + JWT access tokens are confirmed.** Enabling early breaks `/challenge` mid-flight.

### 2. Stop pinning the dashboard to one hub key

- [ ] Signed-in users: hub + history use their token only (no shared key for them)
- [ ] Anonymous: either locked demo source, or require sign-in for `/tail/*`

### 3. Route authorization

- [ ] `AuthorizeRouteView` + `[Authorize]` on `/manage`, `/billing`
- [ ] Sign-in / Sign-out that match Auth enabled state

### 4. Self-serve key lifecycle

- [ ] Create / list / revoke on `/manage` end-to-end after OIDC
- [ ] Live API URL + key header in home / Manage snippets (not localhost)

### 5. Custom domain

- [ ] `app.qiklog.com` (or similar) → Railway web
- [ ] Update Zitadel redirect URIs + CORS + `QikLog__ApiBaseUrl` / docs

### 6. Deploy safety net

- [ ] Post-deploy `make smoke` (or CI job) against production URLs

---

## Path C — paid public (after B dogfooded)

- [ ] Stripe **live** mode + real Pro checkout
- [ ] Usage limits proven under real keys
- [ ] Redis / SignalR backplane if multi-replica
- [ ] Marketing push / public signup

**Explicitly not now:** OpenTelemetry, OAuth device flow for CLI ingest, alerts/webhooks/search.
