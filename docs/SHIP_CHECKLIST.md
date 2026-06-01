# QikLog — path to sellable (ASAP)

What you have **today**: working Hello World (ingest, live tail, CLI, Docker, CI). Good for demos and dogfooding, not yet a product you can charge for.

## Tier 0 — Demo on the internet (~1 day)

Ship a public URL so prospects can try the tail page.

| Item | Status | Notes |
|------|--------|-------|
| Azure infra script (idempotent) | Done | `make azure-setup` |
| Azure deploy script | Done | `make azure-deploy` |
| CORS for production Web origin | Done | `Cors__AllowedOrigins__0` from deploy |
| Custom domain `app.qiklog.com` | Manual | DNS + `az containerapp hostname` per AZURE_DEPLOY.md |
| README / status line | Done | `make verify` documents local gate |
| Marketing www | Done | `www/` Astro site — deploy to www.qiklog.com |
| Unit + API tests | Done | Core + `QikLog.Api.Tests` (27 tests) |

**Cost:** ~\$5 ACR + \$0–5 Container Apps (free grant) if `SKIP_POSTGRES=true`.

## Tier 1 — Minimum paid product (~2–3 weeks)

Enough to sell **Pro \$9/mo** to indie devs (API keys + accounts + persistence). Skip alerts and search at first.

| # | Ticket | Why it blocks revenue |
|---|--------|------------------------|
| 11 | API key auth on ingest | **Done** — Argon2id, Bearer / X-Api-Key, per-key rate limit |
| 10 | Postgres schema + migrations | **Partial** — `log_entries` only; tenants/keys later |
| 14 | Persist logs | **Done** — write on ingest; no history UI yet |
| 12 | Identity (register/login) | Who owns the tenant |
| 13 | Source management UI | Create/revoke sources and keys |
| 30–31 | Stripe Checkout + Portal | Actually collect money |
| 33 | Usage limits (basic) | Cap free tier, upsell Pro |
| 53 | ToS + Privacy (generator) | Required before taking payments |
| 34 | Landing page (qiklog.com) | Conversion; can be static HTML |

**Defer for v1.1:** #15 search, #16 Redis buffer, #17 public OpenAPI polish, Phase 3 alerts.

## Tier 2 — Launch polish (~1–2 weeks after Tier 1)

| # | Ticket |
|---|--------|
| 50 | GitHub Actions → ACR → Container Apps |
| 35 | Docs / quickstart |
| 36 | Show HN / r/dotnet posts |
| 42–43 | Homebrew / Scoop CLI distribution |

## Recommended build order

1. **Deploy demo** — `make azure-setup` then `make azure-deploy`
2. **#10 + #14** — schema + write path on ingest
3. **#11 + #13** — API keys + UI
4. **#12** — Identity
5. **#30–33 + #53** — Stripe + legal
6. **#34** — landing

## Makefile targets (Azure)

```bash
make azure-setup    # RG, ACR, Container Apps env (optional Postgres)
make azure-deploy   # build, push, deploy API + Web
make azure          # both
```

Prerequisites: `az`, `docker`, `.env` from `.env.example`, `az login` (or SP vars).
