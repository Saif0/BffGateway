# Makefile for QiCard BffGateway quick commands

SHELL := /bin/bash
ROOT := $(PWD)

.PHONY: help build clean run-all run-gateway run-provider stop scripts tests load bench provider-load provider-load-quick provider-load-heavy

help:
	@echo "Available commands:"
	@echo "  make build         - Restore and build all projects (Debug)"
	@echo "  make clean         - Clean build outputs"
	@echo "  make run-all       - Start Mock Provider (5001) and BFF Gateway (5000)"
	@echo "  make run-gateway   - Start only BFF Gateway on :5000"
	@echo "  make run-provider  - Start only Mock Provider on :5001"
	@echo "  make stop          - Stop background services started by run-all"
	@echo "  make scripts       - Run endpoint smoke tests script"
	@echo "  make tests         - Alias for scripts (endpoint tests)"
	@echo "  make load          - Run k6 load test (requires k6 installed)"
	@echo "  make provider-load - Run provider k6 test (env tunables below)"
	@echo "  make provider-load-quick - Quick 1m provider test"
	@echo "  make provider-load-heavy - Heavier 10m provider test"
	@echo "  make bench         - Run benchmarks project"

# Provider load test defaults (override like: make provider-load AUTH_RPS=300 DURATION=10m)
PROVIDER_BASE_URL ?= http://localhost:5001
AUTH_RPS ?= 200
PAY_RPS ?= 200
DURATION ?= 5m
PREALLOC_VUS ?= 50
MAX_VUS ?= 500

build:
	dotnet build --nologo -clp:Summary -v:m

clean:
	dotnet clean

# Background PIDs file
PIDS_FILE := .services.pids

run-all:
	@echo "Starting services..."
	@bash scripts/start-services.sh & echo $$! > $(PIDS_FILE)
	@echo "Services launcher PID saved to $(PIDS_FILE)"

run-gateway:
	@echo "Starting BFF Gateway on http://localhost:5000 ..."
	@cd src/BffGateway.WebApi && dotnet run --urls "http://localhost:5000"

run-provider:
	@echo "Starting Mock Provider on http://localhost:5001 ..."
	@cd src/MockProvider && dotnet run --urls "http://localhost:5001"

stop:
	@if [ -f $(PIDS_FILE) ]; then \
		PIDS=$$(cat $(PIDS_FILE)); \
		echo "Stopping services for PIDs: $$PIDS"; \
		kill $$PIDS || true; \
		rm -f $(PIDS_FILE); \
	else \
		echo "No $(PIDS_FILE) found. If services started manually, kill them separately."; \
	fi

scripts:
	bash scripts/test-endpoints.sh

tests: scripts

load:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	k6 run performance/load-test.js

provider-load:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "Running provider load: $(AUTH_RPS)/s auth, $(PAY_RPS)/s pay for $(DURATION) against $(PROVIDER_BASE_URL)"
	PROVIDER_BASE_URL=$(PROVIDER_BASE_URL) \
	AUTH_RPS=$(AUTH_RPS) \
	PAY_RPS=$(PAY_RPS) \
	DURATION=$(DURATION) \
	PREALLOC_VUS=$(PREALLOC_VUS) \
	MAX_VUS=$(MAX_VUS) \
	k6 run performance/provider-load-test.js

provider-load-quick:
	$(MAKE) provider-load AUTH_RPS=10 PAY_RPS=10 DURATION=1m PREALLOC_VUS=20 MAX_VUS=50

provider-load-heavy:
	$(MAKE) provider-load AUTH_RPS=500 PAY_RPS=500 DURATION=10m PREALLOC_VUS=200 MAX_VUS=2000

bench:
	@echo "Running benchmarks..."
	@cd tests/BffGateway.Benchmarks && dotnet run -c Release


