# Deploying QikLog to Azure

Goal: get the Hello World running on Azure for under $25/month so you can put "Azure" on the resume and have a real URL to demo. Production-grade hardening comes later.

## Resources we'll provision

| Resource | Tier | Monthly cost (approx) | Why |
|----------|------|----------------------|-----|
| Azure Container Registry | Basic | $5 | Stores the API + Web Docker images |
| Azure Container Apps | Consumption | $0 within free grant | Hosts both containers |
| Azure Database for PostgreSQL | Burstable B1ms | $13-15 | Smallest managed Postgres |
| Azure Cache for Redis | (skip for MVP) | $0 | Use in-process cache until Phase 2 |
| Front Door / Custom Domain | Free tier | $0 | TLS + custom domain on Container Apps |

Container Apps consumption plan includes a monthly free grant: **180,000 vCPU-seconds and 360,000 GiB-seconds**. A lightly-used app stays inside that grant.

## Prerequisites

- Azure subscription (sign up at azure.microsoft.com — free trial includes $200 credit for 30 days)
- Azure CLI: `brew install azure-cli`
- Docker Desktop running
- Copy `.env.example` → `.env` and set `QIKLOG_SEED`, `ACR_NAME`, etc.

## Automated deploy (idempotent)

Scripts live under `scripts/`. Entry point: `./scripts/main.sh <name>`.

```bash
az login                    # or set AZURE_CLIENT_ID / SECRET / TENANT_ID in .env

make azure-setup            # resource group, ACR, Container Apps env
make azure-deploy           # build linux/amd64 images, push, create/update apps
# or: make azure            # both
```

Re-running `azure-setup` or `azure-deploy` is safe: existing resources are detected and skipped or updated.

Set `SKIP_POSTGRES=false` in `.env` when you are ready for managed Postgres (~$13/mo). Default `true` matches the current app (no persistence yet).

See [SHIP_CHECKLIST.md](SHIP_CHECKLIST.md) for what is left before you can sell subscriptions.

## Custom domain (qiklog.com → Container Apps)

Once everything works on the auto-generated `*.azurecontainerapps.io` URLs, add the custom domain:

```bash
az containerapp hostname add --hostname app.qiklog.com --resource-group $RESOURCE_GROUP --name qiklog-web
az containerapp hostname bind --hostname app.qiklog.com --resource-group $RESOURCE_GROUP --name qiklog-web --environment $ENV_NAME --validation-method CNAME
```

You'll need to add CNAME + TXT records at your DNS provider (currently Vercel — same pattern you used for `jameymcelveen.com`). Container Apps auto-issues a managed certificate.

## Cost-control reflexes

- **Min replicas of 0 for the API** — scales to zero when idle. Cold start is ~5 seconds; acceptable for the API.
- **Min replicas of 1 for the Web** — Blazor Server can't scale to zero (would drop user circuits). One always-on instance.
- **Stop Postgres when not in use** — `az postgres flexible-server stop` while you're developing locally. Restart with `az postgres flexible-server start`. Saves ~$10/mo.
- **Set up a budget alert** — `az consumption budget create` or via portal. $30/mo alert.

## CI/CD wiring (Phase 4)

Skip until you have something worth deploying continuously. When ready, add a `.github/workflows/deploy-azure.yml` that:
1. Builds Docker images on push to `main`
2. Pushes to ACR via `az acr login` with a service principal
3. Updates Container Apps with `az containerapp update --image ...`

Use OIDC federated credentials (not long-lived secrets) — `azure/login@v2` with `client-id` and `tenant-id` only.
