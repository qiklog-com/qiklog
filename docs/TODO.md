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

## DNS consolidation: move nameservers to Vercel

Goal: manage all qiklog.com DNS in Vercel instead of GoDaddy.
Sequence matters; do not flip nameservers first.

- [ ] Decide: is email @qiklog.com in use? (GoDaddy secureserver MX
      records exist.) If unused, drop them during migration; if
      used, migrate MX intact.
- [ ] Add qiklog.com to the Vercel qiklog-www project domains (if
      not already) and open Vercel DNS for the domain.
- [ ] Recreate in Vercel DNS before delegating:
      - CNAME app -> m04szyb0.up.railway.app
      - CNAME api -> 0umysld7.up.railway.app
      - TXT _railway-verify.app -> railway-verify=10dc7bdce30ea8b9ea57acd165ffaa8d1cea3507e670bea742d729e8ef16f082
      - TXT _railway-verify.api -> railway-verify=17728e4ec0d961cf0f762d7bc84e5bc51a1e814e348be858c55d70caa63ae464
      - MX records (only if email is in use)
      - Skip legacy GoDaddy cruft: e/email/ftp/imap/mail/mobilemail/
        pda/pop/smtp/webmail CNAMEs, _domainconnect. Vercel handles
        apex + www itself.
- [ ] Wait for Railway to show both custom domains provisioned.
- [ ] Flip nameservers at GoDaddy to ns1.vercel-dns.com /
      ns2.vercel-dns.com.
- [ ] After propagation: confirm qiklog.com, app.qiklog.com,
      api.qiklog.com, and sign-in all work. Remove Vercel's
      dangling A/www records at GoDaddy is moot once NS flipped.

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
