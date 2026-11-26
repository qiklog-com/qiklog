# Bill memory

## Current state
- New replicant, first session pending. Branch: `azure-move`.
- Production today: Railway (qiklog.up.railway.app, qiklog-api.up.railway.app; app/api.qiklog.com via Cloudflare DNS, records in docs/TODO.md). Marketing www on Vercel.
- Existing Azure tooling: scripts/azure-setup.sh, azure-deploy.sh, docs/AZURE_DEPLOY.md (never fully used).

## Last session
- None yet.

## Status check 2026-11-24 ("I really want to make $1")
- Main has self-serve sign-up (#41), tenant isolation (#29), and Stripe checkout, webhook, and portal (#37/#38), plus stripe-setup.sh. Pricing is Free 10k lines / Pro $9 for 500k.
- Prod is up: app/api/www return 200. /billing redirects to Zitadel sign-in. The webhook answers "invalid signature", so Stripe is enabled. Terms and privacy pages are live.
- Unknown: whether Railway is on sk_test or sk_live. The auto-mode classifier blocked reading Railway vars; ask Jamey instead of retrying.
- Path to first $: activate the Stripe account, run `scripts/stripe-setup.sh --live`, set "failed payments" to cancel/unpaid by hand, then do a real $9 checkout. Azure is not on that path; it adds about $20-35/month.

## Decisions
- None yet.

## Open questions for Jamey
- Is the Azure subscription new (12-month free Postgres B1ms) or existing pay-as-you-go?
- Preferred region (default East US 2).

## Requests for Garfield
- None yet.
