# BFF Gateway (.NET 8) – Auth and Payments

A production‑grade Backend‑for‑Frontend (BFF) service implemented in .NET 8 that fronts an external provider for authentication and payments. It focuses on clean architecture, resiliency with Polly, structured logging, runtime health, observability with OpenTelemetry/Aspire, explicit API versioning with deprecation, and performance validation with k6.

## Highlights

- No custom mapping libraries (e.g., AutoMapper). Hand‑written mappings for maximum performance and clarity
- Global exception handling and middleware that standardize RFC 7807 ProblemDetails without leaking internals
- Serilog structured JSON logs; logs all inbound/outbound HTTP with sensitive data masking; persisted under `logs/`
- OpenTelemetry (+Aspire dashboard via Docker) for traces and metrics; optional Serilog OTLP sink
- Localization-driven messages and validation using FluentValidation; multi-language resources (`en`, `ar`)
- Polly per‑client policies (retry with jitter, circuit breaker, timeouts) using `IHttpClientFactory`
- CQRS with MediatR and Clean Architecture boundaries
- API Versioning (URL segments) with v1 deprecated and v2 current; deprecation headers injected by middleware and reflected in Swagger
- k6 scripts to validate targets (1000 rps, 10m, p95 < 150 ms, error < 1%) and to demonstrate circuit breaker behavior
- Docker Compose and Makefile for easy local runs; MockProvider simulates latency and failures (502/408/429)

## Solution Structure

- `src/BffGateway.WebApi`: Controllers, DI, pipeline, versioning, health, swagger, program entry
- `src/BffGateway.Application`: CQRS commands/handlers, DTOs, interfaces, validators, behaviors
- `src/BffGateway.Infrastructure`: Provider clients, Polly policies, handlers, logging, configuration
- `src/MockProvider`: Local mock provider to simulate real provider behavior and failure modes
- `performanceTesting`: k6 load and circuit-breaker scripts + config
- `tests`: Unit tests for Application layer

## Run locally

### Prerequisites

- .NET 8 SDK
- NodeJS optional (for k6 via binaries) or install k6 CLI
- Docker (optional, for Aspire dashboard and containerized run)

### Quick start (terminal tabs)

1. Start Mock Provider

```bash
make run-provider
# exposes http://localhost:5001 by default (compose uses :5001->:8080)
```

2. Start BFF Gateway

```bash
make run-gateway
# exposes http://localhost:5180 by default (from Makefile)
```

3. Open Swagger (development)

- `http://localhost:5180/swagger`

4. Health

```bash
curl http://localhost:5180/health/live
curl http://localhost:5180/health/ready
```

### Docker Compose (with Aspire dashboard)

```bash
make docker-up
# BFF: http://localhost:5180
# MockProvider: http://localhost:5001
# Aspire dashboard (UI): http://localhost:18888 (traces/metrics)
```

Stop/remove:

```bash
make docker-down
```

## API

### Versions

- v1: Deprecated (still callable); deprecation headers are injected
- v2: Current

### Auth

- v1: `POST /v1/auth/login`
- v2: `POST /v2/auth/login`

Request (v2):

```json
{
  "username": "testuser",
  "password": "PaPassword123"
}
```

Response (v2):

```json
{
  "isSuccess": true,
  "message": "Login successful",
  "token": {
    "accessToken": "<jwt>",
    "expiresAt": "2025-01-01T10:30:00Z",
    "tokenType": "Bearer"
  },
  "user": {
    "username": "testuser"
  }
}
```

### Payments

- v1: `POST /v1/payments` (deprecated)
- v2: `POST /v2/payments`

Request (v2):

```json
{
  "amount": 100.5,
  "currency": "USD",
  "destinationAccount": "ACC123456"
}
```

Response (v2):

```json
{
  "isSuccess": true,
  "message": "Payment processed successfully",
  "paymentId": "c9b9f4d8-27b8-4a6f-bf6c-2b5c7c7c9f90",
  "providerReference": "PROV_20250101_103000_1234",
  "processedAt": "2025-01-01T10:30:00Z"
}
```

### Deprecation headers (v1 only)

- `Deprecation: true`
- `Sunset: Wed, 31 Dec 2025 23:59:59 GMT`
- `Link: </swagger/v2/swagger.json>; rel=successor-version`
- `Warning: 299 - "v1 is deprecated; migrate to v2"`

## Mapping to Provider

Provider endpoints (MockProvider):

- `POST /api/authenticate` with `{ user, pwd }`
- `POST /api/pay` with `{ total, curr, dest }`

BFF normalizes requests and responses:

- Auth: `username/password` → provider `{ user, pwd }` → BFF `{ isSuccess, jwt, expiresAt }` (v1) or token shape (v2)
- Payment: `{ amount, currency, destinationAccount }` → provider `{ total, curr, dest }` → BFF `{ isSuccess, paymentId, providerReference, processedAt }`

No AutoMapper is used; DTOs and mapping are implemented manually for performance and explicitness.

## Resiliency (Polly per client)

Configured in `BffGateway.Infrastructure.DependencyInjection`:

- Retry with exponential backoff and jitter for `5xx/408/HttpRequestException/TimeoutRejectedException`
- Circuit Breaker with open/half‑open/reset events logged
- Timeouts: connection and overall request
- Handlers order: logging → forward headers (Authorization, `X‑Correlation‑ID`) → circuit breaker → retry → timeout

Key settings (default; override via env vars):

- `Provider__TimeoutSeconds = 30`
- `Provider__ConnectTimeoutSeconds = 10`
- `Provider__Retry__MaxRetries = 3`
- `Provider__Retry__BaseDelayMs = 1000`
- `Provider__Retry__MaxJitterMs = 500`
- `Provider__CircuitBreaker__FailureThreshold = 5`
- `Provider__CircuitBreaker__DurationOfBreakSeconds = 30`

## Observability

- Serilog structured JSON logs (console + file). Optional OTLP sink when `Observability:EnableSerilogOtlpSink=true`
- OpenTelemetry traces and metrics when `Observability:EnableOpenTelemetry=true`
- Aspire dashboard via compose (`dashboard` service) for end‑to‑end visibility
- Correlation propagated end‑to‑end with `X‑Correlation‑ID`

Environment flags:

- `Observability__EnableSerilog=true|false`
- `Observability__EnableOpenTelemetry=true|false`
- `Observability__EnableSerilogOtlpSink=true|false`
- `Observability__Otlp__Endpoint=http://dashboard:18889`
- `Observability__Otlp__Protocol=Grpc|HttpProtobuf`

## Security

- No credentials stored; service is stateless
- Forwards inbound `Authorization` header to the provider
- Global exception handler returns safe ProblemDetails; no internal details leaked
- Sensitive data masked in logs (headers: Authorization, cookies; body fields: password, token, cardNumber, cvv, etc.)

## Localization & Validation

- FluentValidation on commands; localized messages (`Resources/Messages.resx` + `Messages.ar.resx`)
- Accept-Language header supported; responses include localized titles/details for ProblemDetails and messages

## Health Endpoints

- `/health/live`: liveness
- `/health/ready`: readiness; includes lightweight provider check and returns 503 when degraded/unhealthy (e.g., circuit open)

## Performance & Load

Targets (sustained with provider avg 80 ms):

- p95 latency < 150 ms
- error rate < 1%
- throughput ~1000 rps for 10 minutes

Run load tests (defaults can be overridden via env):

```bash
# Quick 1m test
make bff-load-quick

# Sustained 10m test
make bff-load-heavy
```

Env knobs for k6 (see `performanceTesting/config.js`):

- `BFF_BASE_URL` (default `http://localhost:5180`)
- `BFF_RPS` (default `1000`)
- `BFF_DURATION` (e.g., `10m`)
- `BFF_PREALLOC_VUS`, `BFF_MAX_VUS`
- `BFF_P95_MS` (default `150`)

Circuit breaker demonstration:

```bash
make circuit-breaker
```

## Configuration

Config via `appsettings*.json` and environment variables. Missing required settings for enabled features fail fast.

- Provider
  - `Provider__BaseUrl` (compose sets `http://mockprovider:8080`)
  - `Provider__TimeoutSeconds`, `Provider__ConnectTimeoutSeconds`
  - `Provider__Retry__MaxRetries`, `Provider__Retry__BaseDelayMs`, `Provider__Retry__MaxJitterMs`
  - `Provider__CircuitBreaker__FailureThreshold`, `Provider__CircuitBreaker__DurationOfBreakSeconds`
- Observability
  - `Observability__EnableSerilog`, `Observability__EnableOpenTelemetry`, `Observability__EnableSerilogOtlpSink`
  - `Observability__Otlp__Endpoint`, `Observability__Otlp__Protocol`
- Logging masking (`LoggingMasking` section)
- Localization (`Localization` section)

Note: This project follows a “no hidden defaults” mindset for critical toggles: if you enable OTLP exporters or sinks, endpoint/protocol must be provided, otherwise startup fails clearly.

## Extending Providers

- Implement `IProviderClient` for the new provider
- Register a named `HttpClient` with specific policies/timeouts
- Add a case in `ProviderClientFactory` (or register via DI)
- Update `appsettings` for base URL and policy tuning

Example endpoints supported today:

- `MockProvider` with `Auth`, `Payments`, and `Ping` for readiness
- Structure allows easy addition (e.g., Stripe) without affecting BFF contracts

## Tests

- Unit tests for Application layer

```bash
make tests-run-all
# or
dotnet test tests/BffGateway.Application.Tests/
```

## Known Limitations

- No rate limiting; would be added for production traffic control
- Circuit breaker is in‑process (per instance); distributed breaker would require external coordination
- v1 kept for demonstration; plan sunset per deprecation headers

## How this meets the assignment

- Clean architecture, CQRS, typed `HttpClient`, Polly policies per client
- Strict timeouts, async end‑to‑end; thread pool not blocked
- Health endpoints for live/ready; readiness reflects provider state and breaker
- Structured logs, correlation, OpenTelemetry traces/metrics, Swagger docs
- Explicit API versioning; v1 deprecated with headers and Swagger labeling; v2 shows contract evolution
- Security: safe errors, no secrets persisted, bearer forwarded
- Performance validated with k6 scripts and thresholds

## Makefile cheatsheet

```bash
make help
make run-provider
make run-gateway
make bff-load-quick
make bff-load-heavy
make circuit-breaker
make docker-up
make docker-down
make tests-run-all
```
