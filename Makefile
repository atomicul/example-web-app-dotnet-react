API_URL ?= http://localhost:5080
SPA_URL ?= http://localhost:5173
OUT ?= out

SOLUTION := backend/TodoApp.slnx
API_PROJECT := backend/TodoApi
FRONTEND := frontend

.DEFAULT_GOAL := help

.PHONY: help install dev dev-backend dev-frontend api build run check clean

help: ## Show this help
	@awk 'BEGIN {FS = ":.*## "} /^[a-z-]+:.*## / {printf "  %-14s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

install: ## Restore the .NET solution and install the frontend packages
	dotnet restore $(SOLUTION)
	pnpm -C $(FRONTEND) install

dev: ## Run the API and the Vite dev server together
	$(MAKE) -j2 dev-backend dev-frontend

dev-backend: ## Run only the API with dotnet watch
	SpaUrl=$(SPA_URL) dotnet watch --project $(API_PROJECT)

dev-frontend: ## Run only the Vite dev server
	API_URL=$(API_URL) pnpm -C $(FRONTEND) dev

api: ## Regenerate the frontend client from the API OpenAPI document
	dotnet build $(API_PROJECT)
	cp $(API_PROJECT)/obj/openapi.json $(FRONTEND)/openapi.json
	pnpm -C $(FRONTEND) generate:api

build: ## Publish the API to out/ with the built SPA in out/wwwroot
	rm -rf $(OUT)
	dotnet publish $(API_PROJECT) -c Release -o $(OUT)
	pnpm -C $(FRONTEND) build
	mkdir -p $(OUT)/wwwroot
	cp -R $(FRONTEND)/dist/. $(OUT)/wwwroot/

run: build ## Build, then serve the API and the SPA together
	cd $(OUT) && dotnet TodoApi.dll --urls $(API_URL)

check: ## Run formatting, build, lint, type checks and tests
	dotnet format $(SOLUTION) --verify-no-changes
	dotnet build $(SOLUTION)
	pnpm -C $(FRONTEND) lint
	pnpm -C $(FRONTEND) format:check
	pnpm -C $(FRONTEND) typecheck
	pnpm -C $(FRONTEND) test

clean: ## Remove build output, including the backend bin and obj directories
	rm -rf $(OUT) $(FRONTEND)/dist backend/*/bin backend/*/obj
