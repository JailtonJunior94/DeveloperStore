SHELL := bash
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := help

MAKEFLAGS += --no-builtin-rules
MAKEFLAGS += --warn-undefined-variables

DOTNET ?= dotnet
DOCKER ?= docker
COMPOSE ?= $(DOCKER) compose
EF := $(DOTNET) tool run dotnet-ef
POSTGRES_DB ?= developerstore
POSTGRES_USER ?= developerstore_app
POSTGRES_PASSWORD ?= developerstore_local_only
ConnectionStrings__DefaultConnection ?= Host=localhost;Port=5432;Database=$(POSTGRES_DB);Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)

export POSTGRES_DB
export POSTGRES_USER
export POSTGRES_PASSWORD
export ConnectionStrings__DefaultConnection

SOLUTION := DeveloperStore.slnx
API_PROJECT := src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj
API_PROJECT_DIR := $(dir $(API_PROJECT))
API_PROJECT_FILE := $(notdir $(API_PROJECT))
ORM_PROJECT := src/DeveloperStore.ORM/DeveloperStore.ORM.csproj
UNIT_TEST_PROJECT := tests/DeveloperStore.Unit/DeveloperStore.Unit.csproj
INTEGRATION_TEST_PROJECT := tests/DeveloperStore.Integration/DeveloperStore.Integration.csproj
FUNCTIONAL_TEST_PROJECT := tests/DeveloperStore.Functional/DeveloperStore.Functional.csproj
POSTGRES_TEST_PROJECT := tests/DeveloperStore.Postgres/DeveloperStore.Postgres.csproj
BDD_TEST_PROJECT := tests/DeveloperStore.BDD/DeveloperStore.BDD.csproj

CONFIGURATION ?= Debug
PUBLISH_CONFIGURATION ?= Release
ASPNETCORE_ENVIRONMENT ?= Development
FROM ?=
TO ?=
NAME ?=
EF_OUTPUT ?= artifacts/migrations/idempotent.sql

.PHONY: help doctor tools restore clean build rebuild run watch format format-check \
	test test-unit test-integration test-functional test-postgres test-bdd coverage \
	verify verify-full publish docker-pull docker-build docker-up docker-up-build \
	docker-down docker-down-volumes docker-logs docker-ps db-up db-logs db-shell \
	migrate migrate-list migrate-add migrate-remove migrate-script guard-NAME

help: ## Show available commands
	@awk 'BEGIN {FS = ":.*## "; printf "\nUsage: make <target>\n\nTargets:\n"} /^[a-zA-Z0-9_.-]+:.*## / {printf "  %-22s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

doctor: ## Show local toolchain versions
	@printf "dotnet: "
	@$(DOTNET) --version
	@printf "docker: "
	@$(DOCKER) --version
	@printf "compose: "
	@$(COMPOSE) version
	@printf "solution: %s\n" "$(SOLUTION)"

tools: ## Restore local .NET tools
	$(DOTNET) tool restore

restore: tools ## Restore NuGet packages
	$(DOTNET) restore $(SOLUTION)

clean: ## Clean build outputs and transient artifacts
	$(DOTNET) clean $(SOLUTION)
	rm -rf TestResults artifacts

build: restore ## Build the solution
	$(DOTNET) build $(SOLUTION) -c $(CONFIGURATION) --no-restore

rebuild: clean build ## Clean and build again

run: restore ## Run the API locally
	cd $(API_PROJECT_DIR) && ASPNETCORE_ENVIRONMENT=$(ASPNETCORE_ENVIRONMENT) $(DOTNET) run --project $(API_PROJECT_FILE) --no-restore

watch: restore ## Run the API locally with hot reload
	cd $(API_PROJECT_DIR) && ASPNETCORE_ENVIRONMENT=$(ASPNETCORE_ENVIRONMENT) $(DOTNET) watch --project $(API_PROJECT_FILE) run --no-restore

format: restore ## Format the solution in place
	$(DOTNET) format $(SOLUTION)

format-check: restore ## Verify formatting without changing files
	$(DOTNET) format $(SOLUTION) --verify-no-changes

test: test-unit test-integration test-functional ## Run the fast local test suites

test-unit: build ## Run unit tests
	$(DOTNET) test $(UNIT_TEST_PROJECT) -c $(CONFIGURATION) --no-build

test-integration: build ## Run integration tests backed by EF InMemory
	$(DOTNET) test $(INTEGRATION_TEST_PROJECT) -c $(CONFIGURATION) --no-build

test-functional: build ## Run functional API tests
	$(DOTNET) test $(FUNCTIONAL_TEST_PROJECT) -c $(CONFIGURATION) --no-build

test-postgres: build ## Run the PostgreSQL validation script and dedicated tests
	./scripts/validate-postgres.sh

test-bdd: build ## Run the BDD suite backed by Testcontainers
	$(DOTNET) test $(BDD_TEST_PROJECT) -c $(CONFIGURATION) --no-build

coverage: restore ## Collect XPlat coverage for unit, integration and functional tests
	$(DOTNET) test $(UNIT_TEST_PROJECT) -c $(CONFIGURATION) --no-restore --collect:"XPlat Code Coverage"
	$(DOTNET) test $(INTEGRATION_TEST_PROJECT) -c $(CONFIGURATION) --no-restore --collect:"XPlat Code Coverage"
	$(DOTNET) test $(FUNCTIONAL_TEST_PROJECT) -c $(CONFIGURATION) --no-restore --collect:"XPlat Code Coverage"

verify: format-check build test ## Run the fast verification suite

verify-full: format-check build test test-bdd test-postgres ## Run the full verification suite

publish: restore ## Publish the API to artifacts/publish
	$(DOTNET) publish $(API_PROJECT) -c $(PUBLISH_CONFIGURATION) --no-restore -o artifacts/publish

docker-pull: ## Pull the container images declared in docker compose
	$(COMPOSE) pull

docker-build: ## Build the API image
	$(COMPOSE) build developerstore.api

docker-up: ## Start the full stack in detached mode
	$(COMPOSE) up -d

docker-up-build: ## Build and start the full stack in detached mode
	$(COMPOSE) up -d --build

docker-down: ## Stop and remove compose services
	$(COMPOSE) down --remove-orphans

docker-down-volumes: ## Stop services and remove named volumes
	$(COMPOSE) down --volumes --remove-orphans

docker-logs: ## Tail docker compose logs
	$(COMPOSE) logs -f

docker-ps: ## Show docker compose service status
	$(COMPOSE) ps

db-up: ## Start only the PostgreSQL service
	$(COMPOSE) up -d developerstore.db

db-logs: ## Tail PostgreSQL logs
	$(COMPOSE) logs -f developerstore.db

db-shell: ## Open psql inside the PostgreSQL container
	$(COMPOSE) exec developerstore.db psql -U "$${POSTGRES_USER:-developerstore_app}" -d "$${POSTGRES_DB:-developerstore}"

infra-up: db-up ## Start only infrastructure (PostgreSQL)

infra-down: docker-down ## Stop infrastructure and compose services

infra-logs: db-logs ## Tail infrastructure logs

migrate: tools ## Apply pending EF Core migrations
	$(EF) database update --project $(ORM_PROJECT) --startup-project $(API_PROJECT)

migrate-list: tools ## List EF Core migrations
	$(EF) migrations list --project $(ORM_PROJECT) --startup-project $(API_PROJECT)

guard-NAME:
	@if [ -z "$(NAME)" ]; then echo "NAME is required. Example: make migrate-add NAME=AddSalesIndexes"; exit 1; fi

migrate-add: guard-NAME tools ## Create a new EF Core migration
	$(EF) migrations add $(NAME) --project $(ORM_PROJECT) --startup-project $(API_PROJECT)

migrate-remove: tools ## Remove the last EF Core migration
	$(EF) migrations remove --project $(ORM_PROJECT) --startup-project $(API_PROJECT)

migrate-script: tools ## Generate an idempotent SQL migration script
	mkdir -p $(dir $(EF_OUTPUT))
	$(EF) migrations script $(FROM) $(TO) --idempotent --output $(EF_OUTPUT) --project $(ORM_PROJECT) --startup-project $(API_PROJECT)
