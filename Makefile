# Makefile for QiCard BffGateway quick commands

SHELL := /bin/bash
ROOT := $(PWD)

.PHONY: help build clean run-all run-gateway run-provider stop scripts tests load bench provider-load provider-load-quick provider-load-heavy bff-load bff-load-quick bff-load-heavy circuit-breaker circuit-breaker-validate circuit-breaker-scenarios circuit-breaker-quick circuit-breaker-all circuit-breaker-full

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
	@echo "  make bff-load         - Run BFF k6 test (env tunables below)"
	@echo "  make bff-load-quick   - Quick 1m BFF test"
	@echo "  make bff-load-heavy   - Heavier 10m BFF test"
	@echo ""
	@echo "Circuit Breaker Testing:"
	@echo "  make circuit-breaker-validate  - Validate circuit breaker setup"
	@echo "  make circuit-breaker-quick     - Quick circuit breaker test"
	@echo "  make circuit-breaker           - Complete 3-phase circuit breaker test"
	@echo "  make circuit-breaker-scenarios - Test multiple scenarios"
	@echo "  make circuit-breaker-all       - Run all circuit breaker tests"
	@echo "  make circuit-breaker-full      - Full test with service checks"
	@echo ""
	@echo "Other:"
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
	@cd src/BffGateway.WebApi && dotnet run -c Release --urls "http://localhost:5000"

run-provider:
	@echo "Starting Mock Provider on http://localhost:5001 ..."
	@cd src/MockProvider && dotnet run -c Release --urls "http://localhost:5001"

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

# BFF load test defaults (override like: make bff-load BFF_RPS=1500 BFF_DURATION=10m)
BFF_BASE_URL ?= http://localhost:5180
BFF_RPS ?= 1000
BFF_DURATION ?= 1m
BFF_PREALLOC_VUS ?= 200
BFF_MAX_VUS ?= 1000

bff-load:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "Running BFF load: $(BFF_RPS)/s for $(BFF_DURATION) against $(BFF_BASE_URL)"
	BFF_BASE_URL=$(BFF_BASE_URL) \
	BFF_RPS=$(BFF_RPS) \
	BFF_DURATION=$(BFF_DURATION) \
	BFF_PREALLOC_VUS=$(BFF_PREALLOC_VUS) \
	BFF_MAX_VUS=$(BFF_MAX_VUS) \
	k6 run performance/load-test.js

bff-load-quick:
	$(MAKE) bff-load BFF_RPS=50 BFF_DURATION=1m BFF_PREALLOC_VUS=20 BFF_MAX_VUS=1000

bff-load-heavy:
	$(MAKE) bff-load BFF_RPS=1000 BFF_DURATION=10m BFF_PREALLOC_VUS=200 BFF_MAX_VUS=2000

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
	$(MAKE) provider-load AUTH_RPS=10 PAY_RPS=10 DURATION=1m PREALLOC_VUS=20 MAX_VUS=1000

provider-load-heavy:
	$(MAKE) provider-load AUTH_RPS=500 PAY_RPS=500 DURATION=10m PREALLOC_VUS=200 MAX_VUS=1000

bench:
	@echo "Running benchmarks..."
	@cd tests/BffGateway.Benchmarks && dotnet run -c Release

# Circuit Breaker Testing Commands
circuit-breaker-validate:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "🔍 Validating circuit breaker setup..."
	k6 run performance/validate-circuit-breaker-setup.js

circuit-breaker:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "🔥 Running complete circuit breaker test (3 phases)..."
	@echo "📊 Monitor BFF Gateway logs for circuit breaker events:"
	@echo "   - 'Circuit breaker OPEN'"
	@echo "   - 'Circuit breaker HALF-OPEN'"
	@echo "   - 'Circuit breaker RESET'"
	k6 run performance/circuit-breaker-test.js

circuit-breaker-scenarios:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "🧪 Testing multiple circuit breaker scenarios..."
	k6 run performance/circuit-breaker-scenarios.js

circuit-breaker-quick:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "⚡ Running quick circuit breaker test..."
	k6 run performance/quick-circuit-breaker-test.js

circuit-breaker-all: circuit-breaker-validate circuit-breaker circuit-breaker-scenarios
	@echo "✅ All circuit breaker tests completed!"

circuit-breaker-full:
	@echo "🚀 Starting full circuit breaker test with service management..."
	@echo "📋 This will:"
	@echo "   1. Check if services are running"
	@echo "   2. Validate setup"
	@echo "   3. Run complete circuit breaker test"
	@echo "   4. Show results and analysis"
	@echo ""
	@bash scripts/test-circuit-breaker.sh

# Tests Run All tests
tests-run-all:
	dotnet test tests/BffGateway.Application.Tests/
	

