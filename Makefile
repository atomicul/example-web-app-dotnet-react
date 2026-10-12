API_URL ?= http://localhost:5080
OUT ?= out

SOLUTION := backend/TodoApp.slnx
API_PROJECT := backend/TodoApi
FRONTEND := frontend

.PHONY: install dev dev-backend dev-frontend api build run check clean

install:
	dotnet restore $(SOLUTION)
	pnpm -C $(FRONTEND) install

dev:
	$(MAKE) -j2 dev-backend dev-frontend

dev-backend:
	dotnet watch --project $(API_PROJECT)

dev-frontend:
	API_URL=$(API_URL) pnpm -C $(FRONTEND) dev

api:
	dotnet build $(API_PROJECT)
	cp $(API_PROJECT)/obj/openapi.json $(FRONTEND)/openapi.json
	pnpm -C $(FRONTEND) generate:api

build:
	rm -rf $(OUT)
	dotnet publish $(API_PROJECT) -c Release -o $(OUT)
	pnpm -C $(FRONTEND) build
	mkdir -p $(OUT)/wwwroot
	cp -R $(FRONTEND)/dist/. $(OUT)/wwwroot/

run: build
	cd $(OUT) && dotnet TodoApi.dll --urls $(API_URL)

check:
	dotnet format $(SOLUTION) --verify-no-changes
	dotnet build $(SOLUTION)
	pnpm -C $(FRONTEND) lint
	pnpm -C $(FRONTEND) format:check
	pnpm -C $(FRONTEND) typecheck
	pnpm -C $(FRONTEND) test

clean:
	rm -rf $(OUT) $(FRONTEND)/dist
