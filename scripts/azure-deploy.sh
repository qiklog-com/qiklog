#!/usr/bin/env bash
# Build images, push to ACR, deploy/update Container Apps. Safe to re-run.

require "azure-functions.sh"

log_banner "Azure deploy (images + Container Apps)"

azure_require_cli
azure_require_config
azure_login_ensure

azure_ensure_resource_group
azure_ensure_acr
azure_ensure_container_apps_env

azure_acr_login
azure_build_and_push_images

# Deploy API first (no CORS yet), then Web, then patch API CORS to Web origin.
azure_deploy_api ""
local_api_fqdn=$(azure_containerapp_fqdn "$API_APP_NAME")
api_base_url="https://${local_api_fqdn}"

azure_deploy_web "$api_base_url"
local_web_fqdn=$(azure_containerapp_fqdn "$WEB_APP_NAME")
web_origin="https://${local_web_fqdn}"

azure_deploy_api "$web_origin"

azure_print_urls
