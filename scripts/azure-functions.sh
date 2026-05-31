# Idempotent Azure CLI helpers. Expects .env variables and functions.sh loaded.

azure_require_cli() {
  require_cmd az
  require_cmd docker
}

azure_require_config() {
  require_var LOCATION
  require_var RESOURCE_GROUP
  require_var ACR_NAME
  require_var ENV_NAME
  require_var API_APP_NAME
  require_var WEB_APP_NAME
  require_var IMAGE_TAG
}

# Returns 0 when logged in and subscription is usable.
azure_is_logged_in() {
  az account show >/dev/null 2>&1
}

# Prefer existing session; optional service principal or user/password from .env.
azure_login_ensure() {
  if azure_is_logged_in; then
    log_ok "Azure CLI already authenticated ($(az account show --query name -o tsv))"
    return 0
  fi

  if [[ -n "${AZURE_CLIENT_ID:-}" && -n "${AZURE_CLIENT_SECRET:-}" && -n "${AZURE_TENANT_ID:-}" ]]; then
    log_step "Logging in with service principal"
    az login \
      --service-principal \
      -u "$AZURE_CLIENT_ID" \
      -p "$AZURE_CLIENT_SECRET" \
      --tenant "$AZURE_TENANT_ID" \
      --only-show-errors
    log_ok "Service principal login succeeded"
    return 0
  fi

  if [[ -n "${AZ_USERNAME:-}" && -n "${AZ_PASSWORD:-}" ]]; then
    log_step "Logging in with AZ_USERNAME / AZ_PASSWORD"
    az login -u "$AZ_USERNAME" -p "$AZ_PASSWORD" --only-show-errors
    log_ok "User login succeeded"
    return 0
  fi

  log_warn "No Azure credentials in .env — run: az login"
  az login
}

azure_ensure_resource_group() {
  if az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log_ok "Resource group exists: $RESOURCE_GROUP"
  else
    log_step "Creating resource group $RESOURCE_GROUP in $LOCATION"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --only-show-errors
    log_ok "Resource group created"
  fi
}

azure_ensure_acr() {
  if az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log_ok "ACR exists: $ACR_NAME"
  else
    log_step "Creating ACR $ACR_NAME (Basic)"
    az acr create \
      --resource-group "$RESOURCE_GROUP" \
      --name "$ACR_NAME" \
      --sku Basic \
      --only-show-errors
    log_ok "ACR created"
  fi

  # Container Apps need registry credentials unless using managed identity (later).
  if [[ "$(az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" --query adminUserEnabled -o tsv)" != "true" ]]; then
    log_step "Enabling ACR admin user (MVP registry auth)"
    az acr update --name "$ACR_NAME" --admin-enabled true --only-show-errors
    log_ok "ACR admin enabled"
  fi
}

azure_ensure_container_apps_env() {
  if az containerapp env show --name "$ENV_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log_ok "Container Apps environment exists: $ENV_NAME"
  else
    log_step "Creating Container Apps environment $ENV_NAME"
    az containerapp env create \
      --name "$ENV_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --location "$LOCATION" \
      --only-show-errors
    log_ok "Container Apps environment created"
  fi
}

azure_ensure_postgres() {
  if [[ "${SKIP_POSTGRES:-true}" == "true" ]]; then
    log_warn "SKIP_POSTGRES=true — skipping Postgres (~\$13/mo). App does not persist logs yet."
    return 0
  fi

  require_var PG_SERVER
  require_var PG_ADMIN
  require_var PG_PASSWORD
  require_var PG_DB

  if az postgres flexible-server show --name "$PG_SERVER" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log_ok "Postgres server exists: $PG_SERVER"
  else
    log_step "Creating Postgres Flexible Server $PG_SERVER (Burstable B1ms — ~\$13/mo)"
    az postgres flexible-server create \
      --resource-group "$RESOURCE_GROUP" \
      --name "$PG_SERVER" \
      --location "$LOCATION" \
      --admin-user "$PG_ADMIN" \
      --admin-password "$PG_PASSWORD" \
      --tier Burstable \
      --sku-name Standard_B1ms \
      --storage-size 32 \
      --version 16 \
      --public-access 0.0.0.0 \
      --only-show-errors
    log_ok "Postgres server created"
  fi

  if az postgres flexible-server db show \
    --resource-group "$RESOURCE_GROUP" \
    --server-name "$PG_SERVER" \
    --database-name "$PG_DB" >/dev/null 2>&1; then
    log_ok "Postgres database exists: $PG_DB"
  else
    log_step "Creating database $PG_DB"
    az postgres flexible-server db create \
      --resource-group "$RESOURCE_GROUP" \
      --server-name "$PG_SERVER" \
      --database-name "$PG_DB" \
      --only-show-errors
    log_ok "Postgres database created"
  fi
}

azure_acr_login() {
  log_step "Logging Docker into $ACR_NAME.azurecr.io"
  az acr login --name "$ACR_NAME" --only-show-errors
  log_ok "Docker authenticated to ACR"
}

azure_build_and_push_images() {
  local registry="${ACR_NAME}.azurecr.io"
  local api_image="${registry}/qiklog-api:${IMAGE_TAG}"
  local web_image="${registry}/qiklog-web:${IMAGE_TAG}"
  local root="$SCRIPT_DIR/.."

  log_step "Building API image (linux/amd64)"
  docker build --platform linux/amd64 \
    -t "$api_image" \
    -f "$root/src/QikLog.Api/Dockerfile" \
    "$root"

  log_step "Building Web image (linux/amd64)"
  docker build --platform linux/amd64 \
    -t "$web_image" \
    -f "$root/src/QikLog.Web/Dockerfile" \
    "$root"

  log_step "Pushing images to ACR"
  docker push "$api_image"
  docker push "$web_image"
  log_ok "Images pushed: $api_image , $web_image"
}

azure_acr_credentials() {
  ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query username -o tsv)
  ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query passwords[0].value -o tsv)
}

azure_containerapp_ensure() {
  local app_name="$1"
  shift
  # Remaining args passed to create (only used when creating).

  if az containerapp show --name "$app_name" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
    log_ok "Container app exists: $app_name (use azure-deploy to update image)"
    return 1
  fi

  log_step "Creating container app $app_name"
  az containerapp create "$@" --only-show-errors
  log_ok "Container app created: $app_name"
  return 0
}

azure_containerapp_update_image() {
  local app_name="$1"
  local image="$2"
  log_step "Updating $app_name → $image"
  az containerapp update \
    --name "$app_name" \
    --resource-group "$RESOURCE_GROUP" \
    --image "$image" \
    --only-show-errors
  log_ok "Image updated: $app_name"
}

azure_containerapp_fqdn() {
  local app_name="$1"
  az containerapp show \
    --name "$app_name" \
    --resource-group "$RESOURCE_GROUP" \
    --query properties.configuration.ingress.fqdn \
    -o tsv
}

azure_deploy_api() {
  local registry="${ACR_NAME}.azurecr.io"
  local image="${registry}/qiklog-api:${IMAGE_TAG}"
  local cors_origin="${1:-}"

  azure_acr_credentials

  local -a env_vars=(
    "ASPNETCORE_ENVIRONMENT=Production"
    "ASPNETCORE_HTTP_PORTS="
    "ASPNETCORE_HTTPS_PORTS="
  )

  if [[ -n "$cors_origin" ]]; then
    env_vars+=("Cors__AllowedOrigins__0=${cors_origin}")
  fi

  if [[ "${SKIP_POSTGRES:-true}" != "true" ]]; then
    local pg_host="${PG_SERVER}.postgres.database.azure.com"
    env_vars+=("ConnectionStrings__Postgres=Host=${pg_host};Database=${PG_DB};Username=${PG_ADMIN};Password=${PG_PASSWORD};SslMode=Require")
  fi

  local env_args=()
  for ev in "${env_vars[@]}"; do
    env_args+=(--env-vars "$ev")
  done

  if azure_containerapp_ensure "$API_APP_NAME" \
    --name "$API_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --environment "$ENV_NAME" \
    --image "$image" \
    --target-port 5080 \
    --ingress external \
    --registry-server "${registry}" \
    --registry-username "$ACR_USERNAME" \
    --registry-password "$ACR_PASSWORD" \
    --min-replicas 0 \
    --max-replicas 2 \
    --cpu 0.25 \
    --memory 0.5Gi \
    "${env_args[@]}"; then
    : # created
  else
    azure_containerapp_update_image "$API_APP_NAME" "$image"
    if [[ -n "$cors_origin" ]]; then
      az containerapp update \
        --name "$API_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --set-env-vars "Cors__AllowedOrigins__0=${cors_origin}" \
        --only-show-errors
      log_ok "API CORS updated for $cors_origin"
    fi
  fi
}

azure_deploy_web() {
  local registry="${ACR_NAME}.azurecr.io"
  local image="${registry}/qiklog-web:${IMAGE_TAG}"
  local api_base_url="$1"

  azure_acr_credentials

  local -a env_vars=(
    "ASPNETCORE_ENVIRONMENT=Production"
    "ASPNETCORE_HTTP_PORTS="
    "ASPNETCORE_HTTPS_PORTS="
    "QikLog__ApiBaseUrl=${api_base_url}"
    "DataProtection__KeysPath=/var/qiklog/dataprotection"
  )

  local env_args=()
  for ev in "${env_vars[@]}"; do
    env_args+=(--env-vars "$ev")
  done

  if azure_containerapp_ensure "$WEB_APP_NAME" \
    --name "$WEB_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --environment "$ENV_NAME" \
    --image "$image" \
    --target-port 5081 \
    --ingress external \
    --registry-server "${registry}" \
    --registry-username "$ACR_USERNAME" \
    --registry-password "$ACR_PASSWORD" \
    --min-replicas 1 \
    --max-replicas 2 \
    --cpu 0.25 \
    --memory 0.5Gi \
    "${env_args[@]}"; then
    : # created
  else
    azure_containerapp_update_image "$WEB_APP_NAME" "$image"
    az containerapp update \
      --name "$WEB_APP_NAME" \
      --resource-group "$RESOURCE_GROUP" \
      --set-env-vars "QikLog__ApiBaseUrl=${api_base_url}" \
      --only-show-errors
    log_ok "Web API URL updated"
  fi
}

azure_print_urls() {
  local api_fqdn web_fqdn
  api_fqdn=$(azure_containerapp_fqdn "$API_APP_NAME")
  web_fqdn=$(azure_containerapp_fqdn "$WEB_APP_NAME")
  printf '\n%s%s%s\n' "$_C_BOLD" "Deployed URLs" "$_C_RESET"
  printf '  API:  https://%s\n' "$api_fqdn"
  printf '  Web:  https://%s\n' "$web_fqdn"
  printf '  Tail: https://%s/tail/demo\n' "$web_fqdn"
  printf '\n  Test: curl -X POST https://%s/v1/logs -H "Content-Type: application/json" \\\n' "$api_fqdn"
  printf '    -d '"'"'{"source":"demo","level":"info","message":"hello from azure"}'"'"'\n\n'
}
