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
- Azure CLI installed locally: `brew install azure-cli` (or download for Windows)
- Docker Desktop running

## One-time setup

```bash
# Log in
az login

# Set variables (customize)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
LOCATION=eastus2
RESOURCE_GROUP=qiklog-prod
ACR_NAME=qiklogacr$RANDOM  # must be globally unique, lowercase alphanumeric
ENV_NAME=qiklog-env
PG_SERVER=qiklog-pg-$RANDOM
PG_ADMIN=qiklogadmin
PG_PASSWORD='ChangeMe!StrongP@ssw0rd'  # use a generated value; store in 1Password
PG_DB=qiklog

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create ACR
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic
az acr login --name $ACR_NAME

# Create Container Apps environment
az containerapp env create \
  --name $ENV_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Create Postgres Flexible Server (smallest burstable tier)
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $PG_SERVER \
  --location $LOCATION \
  --admin-user $PG_ADMIN \
  --admin-password "$PG_PASSWORD" \
  --tier Burstable \
  --sku-name Standard_B1ms \
  --storage-size 32 \
  --version 16 \
  --database-name $PG_DB \
  --public-access 0.0.0.0
```

## Build and push images

From the repo root:

```bash
# Build for linux/amd64 (Container Apps runs amd64)
docker build --platform linux/amd64 -t $ACR_NAME.azurecr.io/qiklog-api:latest -f src/QikLog.Api/Dockerfile .
docker build --platform linux/amd64 -t $ACR_NAME.azurecr.io/qiklog-web:latest -f src/QikLog.Web/Dockerfile .

# Push
docker push $ACR_NAME.azurecr.io/qiklog-api:latest
docker push $ACR_NAME.azurecr.io/qiklog-web:latest
```

## Deploy to Container Apps

```bash
# API
az containerapp create \
  --name qiklog-api \
  --resource-group $RESOURCE_GROUP \
  --environment $ENV_NAME \
  --image $ACR_NAME.azurecr.io/qiklog-api:latest \
  --target-port 5080 \
  --ingress external \
  --registry-server $ACR_NAME.azurecr.io \
  --min-replicas 0 \
  --max-replicas 2 \
  --cpu 0.25 --memory 0.5Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Postgres="Host=$PG_SERVER.postgres.database.azure.com;Database=$PG_DB;Username=$PG_ADMIN;Password=$PG_PASSWORD;SslMode=Require"

# Grab the API URL
API_URL=$(az containerapp show --name qiklog-api --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)
echo "API: https://$API_URL"

# Web (points at API)
az containerapp create \
  --name qiklog-web \
  --resource-group $RESOURCE_GROUP \
  --environment $ENV_NAME \
  --image $ACR_NAME.azurecr.io/qiklog-web:latest \
  --target-port 5081 \
  --ingress external \
  --registry-server $ACR_NAME.azurecr.io \
  --min-replicas 1 \
  --max-replicas 2 \
  --cpu 0.25 --memory 0.5Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    QikLog__ApiBaseUrl="https://$API_URL"

WEB_URL=$(az containerapp show --name qiklog-web --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)
echo "Web: https://$WEB_URL"
```

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
