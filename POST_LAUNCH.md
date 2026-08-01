# Launch readiness — Garfield (2026-08-01)

**Working checklist:** [`docs/LAUNCH_CHECKLIST.md`](docs/LAUNCH_CHECKLIST.md)

Honest split: **invite beta is already launched.** What is left depends which
“launch” you mean. Do not treat this list as one sprint.

| Bar | Status | One-liner |
|-----|--------|-----------|
| **A — Interview / carnival link** | **Done** | Public HTTPS, CLI + API key ingest, live tail, intentional home |
| **B — Soft launch (friends self-serve)** | **Not done** | They can sign up, create a key, see *their* logs — without you in Postgres |
| **C — Paid public launch** | **Not done** | B + Stripe live + custom domain + you would not be embarrassed |

My recommendation: **stay on A**, close the B blockers below in order, and do
**not** start C until someone outside your head has used B for a week.

---

## Already true (do not rebuild)

- Web https://qiklog.up.railway.app · API https://qiklog-api.up.railway.app
- Auth enforcement **on**; anon ingest **401**
- CLI ships with `QIKLOG_API_KEY` → `Authorization: Bearer`
- Live tail under bootstrap tenant (hub key on Web)
- Demo banner, brand shell, circuit/dispose/dialog freezes fixed through `v0.9.10`
- 72/72 offline tests green; Stripe still TEST (correct for now)
- OpenTelemetry **tabled** — still tabled

---

## Path B — what actually blocks soft launch

Ordered by “stranger can use the product without DMing you.”

### 1. OIDC on for real (the critical path)

Zitadel is wired but `QikLog__Auth__Enabled=false`. Without this, `/manage` is
dead for customers (JWT-only), and every key is an ops favor.

Turn-on checklist (all required):

1. Zitadel app: **JWT** access tokens, audience `qiklog-api`
2. Exact HTTPS redirect URI allowlisted (no trailing-slash mismatch)
3. Set `QikLog__Auth__Enabled=true` (+ ClientSecret / Authority) on **Web and API**
4. Set `ZitadelOrgId` on bootstrap tenant `11bb1044-…` so first login does not
   create a *second* tenant and orphan the hub key
5. Smoke: Sign in → Manage → create key → CLI send → `/tail/{source}` for *that*
   tenant

**Garfield opinion:** This is 80% of “launch.” Everything else is polish until
someone can mint their own key in the UI.

### 2. Stop pinning the dashboard to one hub key

`QikLog__HubApiKey` makes every anonymous visitor’s tail page show *your*
tenant. Fine for demo; fatal for customer #2.

- Signed-in: forward the user’s access token (or a per-session credential) to
  hub + history
- Anonymous: either locked demo source with a clear “this is sample data” path,
  or require sign-in to open `/tail/*`

### 3. Route authorization

`/manage` and `/billing` should not pretend to work while anonymous. Needs
`AuthorizeRouteView` in `Routes.razor` + `[Authorize]` on those pages. Small
change, big honesty win. Drop or gate Sign in until OIDC is actually on.

### 4. Self-serve key lifecycle that matches the story

Today keys work; the *UI path* does not without JWT. Once OIDC is on:

- Create / list / revoke on `/manage` end-to-end
- Home / Manage curl snippets should show the **live** API URL and key header,
  not `localhost:5080`

### 5. Custom domain (credibility, not code)

`app.qiklog.com` (or similar) → Railway. Interviewers forgive
`*.up.railway.app` once; paying strangers less so. Marketing `www` already
exists — keep product and marketing URLs coherent.

### 6. Deploy safety net

`make smoke` exists but is not a deploy gate. Wire it (or a Railway health
check + one post-deploy smoke) so the next auth flip does not silently 500
history/hub again.

---

## Path C — paid launch (after B has been dogfooded)

Do **not** start these to “feel launched.”

| Item | Why it waits |
|------|----------------|
| Stripe **live** mode + real Pro checkout | No self-serve tenants yet; TEST is correct |
| Usage limits enforced in anger | Already coded; prove under real keys first |
| Redis hot buffer / SignalR backplane | Single instance is fine until multi-replica |
| Alerts / webhooks / search | Explicitly later tiers |
| OAuth **device flow** for CLI | Wrong tool for `send` / `tail-file`; API keys stay |
| Public signup marketing push | After B works for cold friends |

---

## Small product debts (nice, not launch-blocking)

Worth doing when you are already in the file — none of these block B.

- **Tail auto-scroll is a no-op** — toggle bound, never scrolls (tiny JS interop)
- **Header/hero blinking caret** — keep blink on the icon; static block in-app
  chrome (we already agreed it is a bit loud on every page)
- **API `UseForwardedHeaders`** — Web has it; API does not. Revisit if websockets
  misbehave
- **API won’t start in Development** — scoped `TenantAuthenticationService` in
  middleware ctor fails scope validation; Production-only today
- **Home still teaches localhost curl** — fine for local; wrong on the live app
- **Coverage ~45% Api** — raise when touching auth, not as a launch ritual

---

## What I would do next (opinionated)

1. **This week:** Zitadel JWT + audience + enable Auth + bind `ZitadelOrgId` +
   one full Sign-in → key → CLI → tail loop on a *second* browser profile.
2. **Same week:** Authorize Manage/Billing; stop anonymous hub-key tail (or
   clearly label a single demo source).
3. **Next:** `app.qiklog.com` + post-deploy smoke.
4. **Only then:** talk about Stripe live and “launch” tweets.

If carnival / interviews only need a clickable wow: you already have it. Link
https://qiklog.up.railway.app/tail/demo , ship a line with the CLI key, watch it
land. Do not apologize for invite-only — apologize if Manage pretends to work.

---

## Deferred log (ship-day notes, still valid)

### Auth / identity

- OAuth device flow for interactive CLI commands only — not for ingest agents
- Public signup copy: honest “coming soon” until Path B item 1 is green

### Tail / SignalR

- Auto-scroll JS interop
- Shared hub API key → per-user credentials (Path B item 2)

### Ops

- OpenTelemetry — v2, do not start
- Stripe TEST until Path C
- `make smoke` in deploy (Path B item 6)

## Ship-day facts

- Live: web https://qiklog.up.railway.app · api https://qiklog-api.up.railway.app
- Latest relevant tags: `v0.9.10-page-unresponsive`, `v0.9.9-api-key-only`,
  `v0.9.8-tail-circuit`
- Auth enforcement **on**; OIDC **off**
- Bootstrap tenant `QikLog Bootstrap` (`11bb1044-…`); hub key prefix `l6qrq16m`;
  CLI ship-day key prefix `0cfxlvkg` (plaintext shown once at create)
