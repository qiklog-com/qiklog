# QikLog — developer tasks
# Run `make` or `make help` for targets. Disable color: NO_COLOR=1 make …

SHELL        := /bin/bash
.SHELLFLAGS  := -eu -o pipefail -c
.DEFAULT_GOAL := help

ROOT         := $(abspath $(dir $(lastword $(MAKEFILE_LIST))))
SLN          := $(ROOT)/QikLog.sln
COMPOSE      := docker compose -f $(ROOT)/docker-compose.yml
CONFIG       ?= Release

API_URL      := http://localhost:5080
WEB_URL      := http://localhost:5081

# Live deployment targeted by `make smoke`. Override per environment.
SMOKE_WEB_URL ?= https://qiklog.up.railway.app
SMOKE_API_URL ?= https://qiklog-api.up.railway.app

# ── Color (disabled when NO_COLOR is set) ─────────────────────────────────────
ifeq ($(NO_COLOR),)
  BOLD  := \033[1m
  DIM   := \033[2m
  CYAN  := \033[36m
  GREEN := \033[32m
  YELLOW:= \033[33m
  RED   := \033[31m
  BLUE  := \033[34m
  MAG   := \033[35m
  RESET := \033[0m
else
  BOLD  :=
  DIM   :=
  CYAN  :=
  GREEN :=
  YELLOW:=
  RED   :=
  BLUE  :=
  MAG   :=
  RESET :=
endif

define banner
	@printf '\n$(BOLD)$(BLUE)▸ %s$(RESET)\n' '$(1)'
endef

define step
	@printf '$(CYAN)→$(RESET) %s\n' '$(1)'
endef

define ok
	@printf '$(GREEN)✓$(RESET) %s\n' '$(1)'
endef

define warn
	@printf '$(YELLOW)!$(RESET) %s\n' '$(1)'
endef

define fail
	@printf '$(RED)✗$(RESET) %s\n' '$(1)' >&2
endef

# ── Help ──────────────────────────────────────────────────────────────────────
.PHONY: help
help: ## Show this help
	@printf '\n$(BOLD)$(MAG)qiklog$(RESET) $(DIM)— available targets$(RESET)\n\n'
	@grep -E '^[a-zA-Z0-9_.-]+:.*##' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*## "}; {printf "  $(CYAN)%-18s$(RESET) %s\n", $$1, $$2}'
	@printf '\n$(DIM)Examples:$(RESET)\n'
	@printf '  make up-d          $(DIM)# stack in background$(RESET)\n'
	@printf '  make demo          $(DIM)# POST a test log$(RESET)\n'
	@printf '  make azure         $(DIM)# setup + deploy$(RESET)\n\n'

# ── .NET ──────────────────────────────────────────────────────────────────────
.PHONY: restore build test test-all smoke smoke-local clean
restore: ## dotnet restore
	$(call banner,Restore)
	$(call step,dotnet restore $(SLN))
	@dotnet restore $(SLN)
	$(call ok,Restore complete)

build: restore ## dotnet build
	$(call banner,Build)
	$(call step,dotnet build $(SLN) -c $(CONFIG) --no-restore)
	@dotnet build $(SLN) -c $(CONFIG) --no-restore
	$(call ok,Build complete)

test: build ## dotnet test (excludes E2E doc capture and live smoke)
	$(call banner,Test)
	$(call step,dotnet test $(SLN) -c $(CONFIG) --no-build --filter 'Category!=E2E&Category!=Smoke')
	@dotnet test $(SLN) -c $(CONFIG) --no-build --verbosity normal --filter 'Category!=E2E&Category!=Smoke'
	$(call ok,Tests passed)

test-all: build ## dotnet test including E2E
	@dotnet test $(SLN) -c $(CONFIG) --no-build --verbosity normal

smoke: ## Smoke-test a live deployment (SMOKE_WEB_URL / SMOKE_API_URL to override)
	$(call banner,Smoke — $(SMOKE_WEB_URL))
	@QIKLOG_SMOKE=1 \
	 QIKLOG_SMOKE_WEB_URL=$(SMOKE_WEB_URL) \
	 QIKLOG_SMOKE_API_URL=$(SMOKE_API_URL) \
	 dotnet test $(ROOT)/tests/QikLog.Smoke.Tests -c $(CONFIG) --verbosity normal
	$(call ok,Smoke passed — $(SMOKE_WEB_URL))

smoke-local: ## Smoke-test the local docker compose stack
	@$(MAKE) smoke SMOKE_WEB_URL=$(WEB_URL) SMOKE_API_URL=$(API_URL)

docs-capture: ## Playwright screenshots → www/public/docs/screenshots
	$(call banner,Doc capture)
	@QIKLOG_E2E=1 dotnet test $(ROOT)/tests/QikLog.DocGen.Tests -c $(CONFIG) --verbosity normal
	$(call ok,Screenshots updated)

demos-record: ## VHS terminal GIFs → www/public/demos (requires vhs)
	$(call banner,VHS demos)
	@command -v vhs >/dev/null || { $(fail) "Install vhs: brew install vhs"; exit 1; }
	@mkdir -p $(ROOT)/www/public/demos
	@cd $(ROOT)/tapes && for t in *.tape; do vhs "$$t"; done
	$(call ok,Demos recorded)

clean: ## Remove bin/obj and dotnet artifacts
	$(call banner,Clean)
	$(call step,Removing bin/ and obj/)
	@find $(ROOT)/src $(ROOT)/tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2>/dev/null || true
	@dotnet clean $(SLN) -c $(CONFIG) --verbosity quiet 2>/dev/null || true
	$(call ok,Clean complete)

# ── Docker Compose ────────────────────────────────────────────────────────────
.PHONY: up up-d down restart logs ps build-images
up: ## Start stack (foreground, build if needed)
	$(call banner,Docker Compose up)
	@cd $(ROOT) && $(COMPOSE) up --build

up-d: ## Start stack detached
	$(call banner,Docker Compose up -d)
	@cd $(ROOT) && $(COMPOSE) up --build -d
	$(call ok,Stack running — API $(API_URL) · Web $(WEB_URL))

down: ## Stop stack and remove containers
	$(call banner,Docker Compose down)
	@cd $(ROOT) && $(COMPOSE) down
	$(call ok,Stack stopped)

restart: ## Recreate containers
	$(call banner,Docker Compose restart)
	@cd $(ROOT) && $(COMPOSE) restart
	$(call ok,Containers restarted)

logs: ## Follow compose logs
	$(call banner,Docker Compose logs)
	@cd $(ROOT) && $(COMPOSE) logs -f

ps: ## Show compose service status
	@cd $(ROOT) && $(COMPOSE) ps

build-images: ## Build API and Web images only
	$(call banner,Build Docker images)
	@cd $(ROOT) && $(COMPOSE) build api web
	$(call ok,Images built)

# ── Quick checks ──────────────────────────────────────────────────────────────
.PHONY: verify health demo open-tail db-migrate
db-migrate: ## Apply EF migrations to local Postgres
	$(call banner,Database migrate)
	@dotnet ef database update --project $(ROOT)/src/QikLog.Infrastructure
	$(call ok,Migrations applied)

.PHONY: verify health demo open-tail
verify: build test health ## Local Phase 1 gate: build, test, HTTP smoke
	$(call ok,Verify passed — open $(WEB_URL)/tail/demo for live SignalR check)

.PHONY: health demo open-tail
health: ## Curl API healthz and Web home
	$(call banner,Health checks)
	$(call step,GET $(API_URL)/healthz)
	@curl -sf $(API_URL)/healthz | cat
	@printf '\n'
	$(call step,GET $(WEB_URL)/)
	@curl -sf -o /dev/null -w 'Web HTTP %{http_code}\n' $(WEB_URL)/
	$(call ok,Health checks OK)

demo: ## POST sample log to source "demo"
	$(call banner,Demo ingest)
	$(call step,POST /v1/logs)
	@curl -sf -o /dev/null -w 'HTTP %{http_code}\n' \
		-X POST $(API_URL)/v1/logs \
		-H 'Content-Type: application/json' \
		-d '{"source":"demo","level":"info","message":"hello from make demo"}'
	$(call ok,Log sent — open $(WEB_URL)/tail/demo)

open-tail: ## Open live tail page in default browser (macOS)
	$(call step,open $(WEB_URL)/tail/demo)
	@open $(WEB_URL)/tail/demo 2>/dev/null || xdg-open $(WEB_URL)/tail/demo 2>/dev/null || \
		$(call warn,Could not open browser — visit $(WEB_URL)/tail/demo)

# ── Azure ─────────────────────────────────────────────────────────────────────
.PHONY: azure azure-setup azure-deploy env-check
env-check:
	@if [ ! -f $(ROOT)/.env ]; then \
		$(call fail,Missing $(ROOT)/.env — copy .env.example and fill in Azure values); \
		exit 1; \
	fi

azure-setup: env-check ## Provision Azure infra (idempotent)
	$(call banner,Azure setup)
	@cd $(ROOT) && ./scripts/main.sh azure-setup
	$(call ok,Infrastructure ready)

azure-deploy: env-check ## Build, push, deploy Container Apps (idempotent)
	$(call banner,Azure deploy)
	@cd $(ROOT) && ./scripts/main.sh azure-deploy
	$(call ok,Deploy complete — URLs printed above)

azure: azure-setup azure-deploy ## Full Azure path: infra then deploy

# ── Marketing site (www/) ─────────────────────────────────────────────────────
WWW_DIR := $(ROOT)/www
.PHONY: www-install www-dev www-build
www-install: ## npm install in www/
	$(call banner,www install)
	@cd $(WWW_DIR) && npm install
	$(call ok,www dependencies installed)

www-dev: www-install ## Astro dev server (port 4321)
	@cd $(WWW_DIR) && npm run dev

www-build: www-install ## Static build → www/dist/
	$(call banner,www build)
	@cd $(WWW_DIR) && npm run build
	$(call ok,www built → www/dist/)

# ── CI parity ─────────────────────────────────────────────────────────────────
.PHONY: ci
ci: build test build-images ## Restore, build, test, and build Docker images
	$(call ok,CI pipeline steps complete)
