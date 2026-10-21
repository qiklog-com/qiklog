---
paths:
  - "infra/**"
  - "scripts/**"
  - ".github/workflows/**"
  - "**/Dockerfile"
  - "docker-compose*.yml"
---
# Infrastructure rules

- This is Bill's lane. If you are not Bill, stop and say what infra change you need instead of making it.
- Bicep for Azure resources; Azure CLI for scripts. No Terraform, no portal-only changes.
- Scripts must be idempotent and use the helpers in `scripts/functions.sh` and `scripts/azure-functions.sh`.
- Secrets live in Key Vault (Azure) or GitHub encrypted secrets; workflows authenticate to Azure with OIDC (`azure/login`), never a client secret.
- Images build for `linux/amd64` and are tagged with the git SHA.
- Every resource gets tags: `app=qiklog`, `env=<env>`, `owner=jamey`.
- Any change that adds monthly cost must state the estimate in the PR.
