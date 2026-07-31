# POST_LAUNCH.md — deferred after ship day (2026-07-31)

Anything discovered on deploy day that is **not** required for the invite-only
live definition of done lives here. Do not start these until the public URL is
stable and the carnival / interview link has been used in anger.

## Auth / identity

- **OIDC for the dashboard** — Zitadel is configured (`signin` / authority) but
  `QikLog__Auth__Enabled=false` on both services. Turning it on needs:
  - JWT access tokens with audience `qiklog-api` (Zitadel defaults to opaque)
  - Exact HTTPS redirect URI allowlist (no trailing-slash mismatch)
  - `ZitadelOrgId` set on the bootstrap tenant so first login does not create a
    second tenant
- **OAuth device flow for the CLI** — correct *only* if/when there are
  interactive user CLI commands (login, billing, key manage from a terminal).
  Ingest agents stay on tenant-scoped API keys (`QIKLOG_API_KEY`). Do not build
  device flow for send/tail-file.
- **Public signup** — “coming soon.” Invite-only: Jamey’s tenant + API keys.

## Tail / SignalR

- **Auto-scroll toggle is a no-op** — bound in UI, never scrolls the viewport.
  Needs a tiny JS interop module; not a Razor rewrite.
- **API `UseForwardedHeaders`** — Web has it; API does not. Live hub works over
  HTTPS today. Revisit if websocket upgrades misbehave behind Railway’s edge.
- **Shared hub API key** — `QikLog__HubApiKey` pins the dashboard to one tenant.
  Forward the signed-in user’s token before customer #2.

## Product / ops

- **OpenTelemetry** — documented v2. Explicitly tabled. Do not start it.
- **Stripe** — stays TEST mode. Billing does not gate “live.”
- **`make smoke` in deploy** — wire so a bad ship fails loudly.
- **Route `[Authorize]`** on `/manage`, `/billing`, `/tail` — needs
  `AuthorizeRouteView`; behavior change, PO call.
- **Home page curl example** still shows localhost in places; marketing site
  (`www`) is the real public story — keep dashboard honest but short.
- **API scoped middleware in Development** —
  `TenantAuthMiddleware` ctor takes scoped `TenantAuthenticationService`;
  Development scope validation crashes startup. Works in Production. Move deps
  into `InvokeAsync` parameters when someone next touches auth DI.

## Ship-day facts (for the next session)

- Live: web https://qiklog.up.railway.app · api https://qiklog-api.up.railway.app
- Tags: `v0.9.8-tail-circuit`, `v0.9.9-api-key-only`
- Auth enforcement **on**; OIDC **off**. Ingest/hub/history accept API keys.
- Bootstrap tenant `QikLog Bootstrap` (`11bb1044-…`). Keys: hub prefix
  `l6qrq16m`, CLI ship-day prefix `0cfxlvkg` (plaintext only shown once at create).
