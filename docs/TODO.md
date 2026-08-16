# QikLog TODO

Working list. Updated 2026-08-15. Owner: Jamey. Bill tracks state in
graymatter short-term memory.

## Done recently

- [x] OIDC live in production. Sign in works end to end on
      qiklog.up.railway.app (root cause was Auth Enabled=false).
- [x] app.qiklog.com redirect URIs added in Zitadel.
- [x] Railway TXT verification records added at GoDaddy for
      app.qiklog.com and api.qiklog.com. Waiting on Railway to
      re-check and provision (minutes to an hour).
- [x] Brand adopted: mark SVGs, BRAND.md, MARKETING_NOTES.md in repo
      (untracked, needs commit).
- [x] Railway billing fixed. 3/3 services online.

## Now

- [ ] Confirm Railway provisioned app.qiklog.com and api.qiklog.com
      (warning triangle becomes checkmark in service Networking).
- [ ] Test sign-in on https://app.qiklog.com once provisioned.
- [ ] Commit brand + docs work: docs/BRAND.md, docs/assets/ (SVGs,
      brand-preview.html), docs/MARKETING_NOTES.md, docs/TODO.md.

## DNS consolidation: move nameservers to Cloudflare

Goal: manage all qiklog.com DNS in Cloudflare instead of GoDaddy.
Chosen over Vercel DNS because Cloudflare is DNS as the actual
product (real API/CLI, e.g. flarectl, scoped API tokens), free tier
is full-featured, and it stays platform-agnostic -- Vercel serves
qiklog.com, Railway serves app./api., Cloudflare just points at both
with no lock-in to either host. Sequence matters; do not flip
nameservers first. GoDaddy now has an official CLI too (`gddy`,
github.com/godaddy/cli) -- use it for reading current records /
scripting the nameserver flip instead of the GoDaddy web UI.

- [ ] Decide: is email @qiklog.com in use? (GoDaddy secureserver MX
      records exist.) If unused, drop them during migration; if
      used, migrate MX intact.
- [ ] Create Cloudflare account, add qiklog.com as a zone.
- [ ] Generate a Cloudflare API token scoped to Zone:DNS:Edit for
      just this zone (not account-wide) once ready for CLI-driven
      DNS management. Store in 1Password QikLog-Dev vault.
- [ ] Recreate in Cloudflare DNS before delegating:
      - A @ -> 76.76.21.21 (Vercel, apex)
      - A/CNAME www -> Vercel per their current instructions
      - CNAME app -> m04szyb0.up.railway.app (proxy OFF / DNS only,
        so Railway's own TLS/edge handling isn't interfered with)
      - CNAME api -> 0umysld7.up.railway.app (proxy OFF)
      - TXT _railway-verify.app -> railway-verify=10dc7bdce30ea8b9ea57acd165ffaa8d1cea3507e670bea742d729e8ef16f082
      - TXT _railway-verify.api -> railway-verify=17728e4ec0d961cf0f762d7bc84e5bc51a1e814e348be858c55d70caa63ae464
      - MX records (only if email is in use)
      - Skip legacy GoDaddy cruft: e/email/ftp/imap/mail/mobilemail/
        pda/pop/smtp/webmail CNAMEs, _domainconnect.
- [ ] Wait for Railway to show both custom domains provisioned.
- [ ] Flip nameservers at GoDaddy to the two Cloudflare-assigned
      nameservers (shown in Cloudflare after zone creation).
- [ ] After propagation: confirm qiklog.com, app.qiklog.com,
      api.qiklog.com, and sign-in all work.

## Backlog: SimService (isometric architecture diagrams)

Goal: Cloudcraft-style isometric infra diagrams for QikLog docs and
marketing -- eye-catching differentiator in a saturated dev-tools
market. NOT a quick win with the flat/accessible diagram toolkit
used elsewhere in this repo (no shading, no gradients beyond one per
diagram, dark-mode-safe by design -- fights against the specular 3D
look Cloudcraft has).

Recommended path: build on the free, MIT-licensed `isoflow` npm
library (not isoflow.io, the hosted SaaS, which is $15/editor/month
for team network-doc collaboration -- a different product solving a
different problem). FossFLOW (github.com/stan-smith/FossFLOW,
Unlicense) is a full open-source app built on that same library and
a good reference implementation. SimService's actual work is a
QikLog-branded icon set (rust/paper/ink) and wrapping the library
around our own architecture data, not building or paying for a
diagramming SaaS. Holstered per Qik-thesis rule until there's
bandwidth -- flat SVG diagrams cover the documentation need today.

## Tooling: adopt Stripe's agent skills

Stripe ships installable skills/plugins + an MCP server so coding
agents build more accurate Stripe integrations
(docs.stripe.com/agents). Two separate things:

- [ ] Install for OUR use now: `npm install -g @stripe/cli`, then
      `stripe agent setup` to add stripe-docs and
      stripe-best-practices skills. Makes checkout/webhook wiring
      more accurate than working from memory.
- [ ] Longer-term product idea, holstered per the Qik thesis rule
      (business case first): QikLog could offer the same pattern to
      ITS users -- a skill or MCP server so agents can wire up log
      ingestion into someone's app the way Stripe skills wire up
      payments. Directly aligned with the "try it now" / zero
      friction thesis, just aimed at integrators instead of the
      landing-page visitor. Not a task until QikLog has users.

## Auth phase 2 (work order ready to write)

- [ ] Web-to-API JWT audience fix: Zitadel app token type
      Bearer -> JWT; QikLog__Auth__ApiAudience=383416044909259568
      on api service; code change in WebAuthExtensions.cs to add
      Zitadel project-audience scope. Deploy api + test as one unit.
      Until then Manage/sources use the API key path.
- [ ] Switch ApiBaseUrl and any hardcoded hosts to api.qiklog.com
      everywhere once domains are live (already set on web service).
- [ ] Sign-in test from a machine with no Zitadel session (full
      login UI path, not SSO shortcut).

## Ship checklist remainder

- [ ] Stripe ruling (Jamey): verify test-mode flow only, or go live.
      Standing call is TEST mode ships.
- [ ] Doc drift cleanup: AUTH.md + SHIP_CHECKLIST.md still describe
      the abandoned Azure/self-hosted-Zitadel plan. Rewrite for
      Railway + Zitadel Cloud + Vercel reality.
- [ ] Reconcile docs/BRAND.md vs docs/BRAND_GUIDE.md (two brand
      files, one truth).
- [ ] Delete superseded teal logos (logo-1.jpg, logo-2.jpg) --
      needs Jamey's explicit ok.
- [ ] Apply brand to Blazor web app + landing page (favicon, header
      lockup, OG image) per BRAND.md asset kit.
- [ ] Wire logging/observability check: SignalR live tail smoke test
      on production.

## Marketing build (per MARKETING_NOTES.md, post-auth)

- [ ] Landing page live demo: anonymous ephemeral workspace + key,
      embedded tail pane, claim-by-signin.
- [ ] Serilog sink package on NuGet.
- [ ] Shareable tail links.
