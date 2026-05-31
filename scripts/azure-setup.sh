#!/usr/bin/env bash
# Idempotent Azure infrastructure: resource group, ACR, Container Apps env, optional Postgres.

require "azure-functions.sh"

log_banner "Azure setup (infrastructure)"

azure_require_cli
azure_require_config
azure_login_ensure

azure_ensure_resource_group
azure_ensure_acr
azure_ensure_container_apps_env
azure_ensure_postgres

log_ok "Infrastructure ready — run: make azure-deploy"
