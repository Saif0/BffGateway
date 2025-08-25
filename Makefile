# Makefile for QiCard BffGateway quick commands

SHELL := /bin/bash
ROOT := $(PWD)

.PHONY: help build clean run-gateway run-provider load bench bff-load bff-load-quick bff-load-heavy circuit-breaker tests-run-all docker-up docker-down docker-logs docker-restart docker-build

help:
	@echo "Available commands:"
	@echo "  make build              - Restore and build all projects (Debug)"
	@echo "  make clean              - Clean build outputs"
	@echo "  make run-gateway        - Start only BFF Gateway on :5000"
	@echo "  make run-provider       - Start only Mock Provider on :5001"
	@echo "  make load               - Run k6 load test (requires k6 installed)"
	@echo "  make bff-load           - Run BFF k6 test (env tunables below)"
	@echo "  make bff-load-quick     - Quick 1m BFF test"
	@echo "  make bff-load-heavy     - Heavier 10m BFF test"
	@echo ""
	@echo "Docker Compose:"
	@echo "  make docker-up          - Start all services with Docker Compose"
	@echo "  make docker-down        - Stop and remove Docker containers"
	@echo "  make docker-logs        - Tail Docker Compose logs"
	@echo "  make docker-restart     - Restart all Docker Compose services"
	@echo "  make docker-build       - Build Docker images"
	@echo ""
	@echo "Circuit Breaker Testing:"
	@echo "  make circuit-breaker    - Quick circuit breaker test"
	@echo ""
	@echo "Other:"
	@echo "  make tests-run-all      - Run all unit tests"
	@echo "  make bench              - Run benchmarks project"

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


run-gateway:
	@echo "Starting BFF Gateway on http://localhost:5000 ..."
	@cd src/BffGateway.WebApi && dotnet run -c Release --urls "http://localhost:5000"

run-provider:
	@echo "Starting Mock Provider on http://localhost:5001 ..."
	@cd src/MockProvider && dotnet run -c Release --urls "http://localhost:5001"

load:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	k6 run performanceTesting/load-test.js

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
	k6 run performanceTesting/load-test.js

bff-load-quick:
	$(MAKE) bff-load BFF_RPS=50 BFF_DURATION=1m BFF_PREALLOC_VUS=20 BFF_MAX_VUS=1000

bff-load-heavy:
	$(MAKE) bff-load BFF_RPS=1000 BFF_DURATION=10m BFF_PREALLOC_VUS=200 BFF_MAX_VUS=2000



bench:
	@echo "Running benchmarks..."
	@cd tests/BffGateway.Benchmarks && dotnet run -c Release

# Circuit Breaker Testing Commands
circuit-breaker:
	@command -v k6 >/dev/null 2>&1 || { echo "k6 is not installed. Install from https://k6.io"; exit 1; }
	@echo "⚡ Running quick circuit breaker test..."
	k6 run performanceTesting/circuit-breaker-test.js



# Tests Run All tests
tests-run-all:
	dotnet test tests/BffGateway.Application.Tests/
	


# Docker Compose commands
docker-up:
	@echo "Starting services with Docker Compose..."
	@docker compose up -d

docker-down:
	@echo "Stopping Docker Compose services..."
	@docker compose down

docker-logs:
	@echo "Tailing Docker Compose logs (Ctrl+C to stop)..."
	@docker compose logs -f

docker-restart:
	@$(MAKE) docker-down
	@$(MAKE) docker-up

docker-build:
	@echo "Building Docker images..."
	@docker compose build
