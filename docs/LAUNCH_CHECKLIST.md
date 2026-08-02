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

### 5. Custom domain (GoDaddy DNS — do this next)

**Done on platform side (2026-08-01):**
- [x] Marketing Astro → Vercel project `qiklog-www` (`https://qiklog-www.vercel.app`)
- [x] Vercel domains attached: `qiklog.com`, `www.qiklog.com`
- [x] Railway custom domains created: `app.qiklog.com` (web), `api.qiklog.com` (api)
- [x] Web `QikLog__ApiBaseUrl=https://api.qiklog.com`
- [x] API CORS: `https://app.qiklog.com` + legacy Railway origin

**Broken today:** apex/`www` A/CNAME fall through to dead Amazon IP `184.72.232.223` (hangs). `app`/`api` have no DNS yet. NS stay on GoDaddy (`ns41/42.domaincontrol.com`) — keep them so MX (`secureserver.net`) keeps working.

**GoDaddy → DNS → Manage DNS — delete then add**

Delete (fall-through killers):
- Any **A** on `@` pointing at `184.72.232.223` (or any non-Vercel A)
- Any **CNAME** on `www` → `qiklog.com` / `@`
- **Domain Forwarding** / **Masked Forwarding** / parking page for `qiklog.com` or `www` (if present)

Add (TTL 600):

| Type | Name | Value | Notes |
|------|------|-------|-------|
| A | `@` | `76.76.21.21` | Vercel apex |
| A | `www` | `76.76.21.21` | Vercel www (or CNAME `www` → `cname.vercel-dns.com`) |
| CNAME | `app` | `m04szyb0.up.railway.app` | Railway web — exact target from Railway |
| CNAME | `api` | `0umysld7.up.railway.app` | Railway api — exact target from Railway |

Do **not** point NS at Vercel unless you also move MX; email is on GoDaddy today.

**After DNS propagates (~5–30 min):**
- [ ] `curl -I https://www.qiklog.com` → Vercel 200
- [ ] `curl -I https://app.qiklog.com` → Railway web
- [ ] `curl -I https://api.qiklog.com/health` (or `/`) → Railway api
- [ ] Zitadel redirect URIs: `https://app.qiklog.com/signin-oidc` + post-logout `https://app.qiklog.com/` (when enabling OIDC)
- [ ] `signin.qiklog.com` deferred — keep using `*.zitadel.cloud` until Zitadel custom domain is configured

### 6. Deploy safety net

- [ ] Post-deploy `make smoke` (or CI job) against production URLs

---

## Path C — paid public (after B dogfooded)

- [ ] Stripe **live** mode + real Pro checkout
- [ ] Usage limits proven under real keys
- [ ] Redis / SignalR backplane if multi-replica
- [ ] Marketing push / public signup

**Explicitly not now:** OpenTelemetry, OAuth device flow for CLI ingest, alerts/webhooks/search.
