# BFF Gateway - Backend-for-Frontend Service

A production-grade Backend-for-Frontend (BFF) service built with .NET 8 that provides a unified API for authentication and payment operations, fronting a third-party provider with comprehensive resiliency, observability, and performance characteristics.

## Architecture Overview

This solution follows Clean Architecture principles with clear separation of concerns:

- **WebApi Layer**: Controllers, API versioning, health checks, and HTTP pipeline configuration
- **Application Layer**: CQRS with MediatR, business logic, and validation
- **Infrastructure Layer**: HTTP client implementations, Polly policies, and external service integration

## Features

### Core Functionality

- ✅ **Authentication Endpoint** (`/v1/auth/login`) - Username/password authentication with JWT tokens
- ✅ **Payment Endpoint** (`/v1/payments`) - Payment processing with provider integration
- ✅ **API Versioning** - Support for v1 and v2 endpoints with different response formats
- ✅ **Input Validation** - FluentValidation for request validation
- ✅ **CQRS Pattern** - Command Query Responsibility Segregation with MediatR

### Resiliency & Performance

- ✅ **HTTP Client Policies** - Retry with exponential backoff and jitter
- ✅ **Circuit Breaker** - Automatic failure detection and recovery
- ✅ **Timeouts** - Configurable connection and request timeouts
- ✅ **Load Testing** - k6 scripts with performance thresholds (p95 < 150ms, error rate < 1%)
- ✅ **Benchmarking** - BenchmarkDotNet for serialization performance

### Observability

- ✅ **Health Checks** - Live (`/health/live`) and ready (`/health/ready`) endpoints
- ✅ **Structured Logging** - Serilog with correlation IDs
- ✅ **OpenAPI Documentation** - Swagger UI for API exploration
- ✅ **Request Logging** - HTTP request/response logging with timing

### Security

- ✅ **Input Validation** - Safe error messages without internal details
- ✅ **No Credential Storage** - Stateless service design
- ✅ **Bearer Token Forwarding** - Pass-through authentication to provider

## Quick Start

### Prerequisites

- .NET 8 SDK
- k6 (for load testing)

### Running the Services

1. **Start the Mock Provider** (Terminal 1):

```bash
cd src/MockProvider
dotnet run
```

The mock provider will start on http://localhost:5001

2. **Start the BFF Gateway** (Terminal 2):

```bash
cd src/BffGateway.WebApi
dotnet run
```

The BFF Gateway will start on http://localhost:5000

3. **Verify Health Checks**:

```bash
# Liveness check
curl http://localhost:5000/health/live

# Readiness check (includes provider connectivity)
curl http://localhost:5000/health/ready
```

### API Usage Examples

#### Authentication (v1)

```bash
curl -X POST http://localhost:5000/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "password123"
  }'
```

Response:

```json
{
  "isSuccess": true,
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-15T10:30:00.000Z"
}
```

#### Authentication (v2) - Enhanced Response Format

```bash
curl -X POST http://localhost:5000/v2/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "password123"
  }'
```

Response:

```json
{
  "success": true,
  "token": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-15T10:30:00.000Z",
    "tokenType": "Bearer"
  },
  "user": {
    "username": "testuser"
  }
}
```

#### Payment Processing

```bash
curl -X POST http://localhost:5000/v1/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 100.50,
    "currency": "USD",
    "destinationAccount": "ACC123456"
  }'
```

Response:

```json
{
  "isSuccess": true,
  "paymentId": "12345678-1234-1234-1234-123456789012",
  "providerReference": "PROV_20240115103000_1234",
  "processedAt": "2024-01-15T10:30:00.000Z"
}
```

## Configuration

### Environment Variables

| Variable                                           | Default                 | Description                       |
| -------------------------------------------------- | ----------------------- | --------------------------------- |
| `Provider__BaseUrl`                                | `http://localhost:5001` | Mock provider base URL            |
| `Provider__TimeoutSeconds`                         | `30`                    | HTTP request timeout              |
| `Provider__ConnectTimeoutSeconds`                  | `10`                    | Connection timeout                |
| `Provider__Retry__MaxRetries`                      | `3`                     | Maximum retry attempts            |
| `Provider__Retry__BaseDelayMs`                     | `1000`                  | Base retry delay                  |
| `Provider__CircuitBreaker__FailureThreshold`       | `5`                     | Circuit breaker failure threshold |
| `Provider__CircuitBreaker__DurationOfBreakSeconds` | `30`                    | Circuit breaker open duration     |

### Configuration Files

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides

## Performance Testing

### Load Testing with k6

Run the comprehensive load test that simulates 1000 concurrent users:

```bash
cd performance
k6 run load-test.js
```

**Performance Targets:**

- **Throughput**: 1000 requests/second sustained for 10 minutes
- **Latency**: p95 < 150ms
- **Error Rate**: < 1%
- **Provider Response**: Average 80ms

The test includes:

- Authentication endpoint testing (v1 and v2)
- Payment endpoint testing
- Health check validation
- Error rate monitoring
- Response time tracking

### Micro-benchmarks

Run BenchmarkDotNet tests for serialization performance:

```bash
cd tests/BffGateway.Benchmarks
dotnet run -c Release
```

## API Versioning Strategy

### Current Versions

- **v1**: Original API format with simple response structures
- **v2**: Enhanced API format with structured token and user information

### Version Selection

You can specify the API version in two ways:

1. **URL Segment** (Recommended):

   ```
   GET /v1/auth/login
   GET /v2/auth/login
   ```

2. **Header-based**:
   ```
   X-Api-Version: 1.0
   X-Api-Version: 2.0
   ```

### Deprecation Strategy

When introducing breaking changes:

1. **Phase 1**: Introduce new version (v2) alongside existing (v1)
2. **Phase 2**: Mark old version as deprecated in OpenAPI spec
3. **Phase 3**: Add deprecation warnings to response headers
4. **Phase 4**: Remove deprecated version after migration period

### Migration Example: v1 to v2

**v1 Login Response:**

```json
{
  "isSuccess": true,
  "jwt": "token...",
  "expiresAt": "2024-01-15T10:30:00.000Z"
}
```

**v2 Login Response:**

```json
{
  "success": true,
  "token": {
    "accessToken": "token...",
    "expiresAt": "2024-01-15T10:30:00.000Z",
    "tokenType": "Bearer"
  },
  "user": {
    "username": "testuser"
  }
}
```

## Health Endpoints

### Liveness Check (`/health/live`)

- **Purpose**: Indicates if the application is running
- **Checks**: Basic application health
- **Use Case**: Kubernetes liveness probe

### Readiness Check (`/health/ready`)

- **Purpose**: Indicates if the application is ready to serve traffic
- **Checks**: Application health + provider connectivity
- **Use Case**: Kubernetes readiness probe, load balancer health checks

### Health States

- **Healthy**: All checks pass
- **Degraded**: Application is running but provider has issues
- **Unhealthy**: Critical failures detected

Example responses:

**Healthy:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "self": {
      "status": "Healthy"
    },
    "provider": {
      "status": "Healthy",
      "description": "Provider is responding"
    }
  }
}
```

**Degraded:**

```json
{
  "status": "Degraded",
  "totalDuration": "00:00:00.0567890",
  "entries": {
    "self": {
      "status": "Healthy"
    },
    "provider": {
      "status": "Degraded",
      "description": "Provider is not responding properly"
    }
  }
}
```

## Resiliency Patterns

### Retry Policy

- **Strategy**: Exponential backoff with jitter
- **Conditions**: HTTP 5xx, 408, HttpRequestException, TimeoutRejectedException
- **Max Attempts**: 3 (configurable)
- **Base Delay**: 1000ms with up to 500ms jitter

### Circuit Breaker

- **Failure Threshold**: 5 consecutive failures
- **Break Duration**: 30 seconds
- **Sampling Window**: 60 seconds
- **Minimum Throughput**: 10 requests

### Timeouts

- **Connection Timeout**: 10 seconds
- **Request Timeout**: 30 seconds
- **Overall Policy Timeout**: 30 seconds

## Error Handling

### Error Response Format

All errors return consistent problem details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Username is required",
  "instance": "/v1/auth/login"
}
```

### Error Categories

1. **Validation Errors** (400): Invalid input data
2. **Authentication Errors** (401): Invalid credentials
3. **Provider Errors** (502): External service failures
4. **Circuit Breaker Open** (503): Temporary service unavailability
5. **Timeout Errors** (504): Request timeout exceeded

## Logging

### Structured Logging

All logs include:

- **Timestamp**: ISO 8601 format
- **Level**: Debug, Information, Warning, Error, Fatal
- **Message**: Human-readable description
- **Properties**: Structured data (user ID, correlation ID, etc.)
- **Exception**: Full exception details when applicable

### Log Correlation

Each request gets a unique correlation ID that flows through:

- HTTP request logging
- Application logs
- Provider calls
- Error logs

Example log entry:

```json
{
  "@timestamp": "2024-01-15T10:30:00.123Z",
  "@level": "Information",
  "@message": "Processing login request for username: {Username}",
  "Username": "testuser",
  "CorrelationId": "abc123-def456-ghi789",
  "RequestPath": "/v1/auth/login",
  "RequestMethod": "POST"
}
```

## Testing Strategy

### Load Testing Methodology

1. **Baseline Performance**: Single user, single request
2. **Ramp-up Testing**: Gradual increase to target load
3. **Sustained Load**: Maintain target load for duration
4. **Stress Testing**: Beyond normal capacity
5. **Recovery Testing**: Service recovery after failures

### Test Scenarios

1. **Happy Path**: Normal authentication and payment flows
2. **Error Conditions**: Invalid inputs, provider failures
3. **Circuit Breaker**: Sustained failures and recovery
4. **Version Compatibility**: Both v1 and v2 endpoints
5. **Health Checks**: Liveness and readiness validation

## Extending the Service

### Adding a New Provider

1. **Implement Interface**: Create new `IProviderClient` implementation
2. **Add Configuration**: Provider-specific settings
3. **Register Service**: Update DI configuration
4. **Add Health Check**: Provider-specific health validation
5. **Update Tests**: Load tests and benchmarks

Example:

```csharp
public class NewProviderClient : IProviderClient
{
    public async Task<ProviderAuthResponse> AuthenticateAsync(
        ProviderAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implementation for new provider
    }
}
```

### Adding New Endpoints

1. **Define Contract**: Request/response models
2. **Create Command**: MediatR command and handler
3. **Add Validation**: FluentValidation rules
4. **Implement Controller**: API endpoint with versioning
5. **Update Documentation**: OpenAPI specification
6. **Add Tests**: Unit tests and load tests

## Known Limitations

1. **Provider Timeout**: Fixed 5-second timeout for test scenarios
2. **Memory Usage**: No persistent state, but HTTP client pooling
3. **Correlation IDs**: Basic implementation without distributed tracing
4. **Circuit Breaker**: Per-instance, not distributed
5. **Rate Limiting**: Not implemented (would be added for production)

## Production Considerations

### Deployment

- **Container**: Docker image with health checks
- **Kubernetes**: Deployment with liveness/readiness probes
- **Load Balancer**: Health check integration
- **Monitoring**: Application metrics and logging

### Security

- **HTTPS**: TLS termination at load balancer
- **API Gateway**: Rate limiting and authentication
- **Secrets**: External configuration for sensitive data
- **CORS**: Configure for frontend domains

### Scaling

- **Horizontal**: Stateless design supports multiple instances
- **Resource Limits**: CPU and memory limits in containers
- **Connection Pooling**: HTTP client connection management
- **Caching**: Response caching for frequently accessed data

## Development Setup

### IDE Configuration

- **Visual Studio**: Solution file included
- **VS Code**: Recommended extensions for C# development
- **JetBrains Rider**: Full solution support

### Debugging

- **Breakpoints**: Full debugging support in all layers
- **Logging**: Structured logs in console during development
- **Health Checks**: Verify service dependencies

### Code Quality

- **Analyzers**: Built-in .NET analyzers enabled
- **Formatting**: EditorConfig for consistent style
- **Testing**: Unit tests for critical business logic

## Support and Maintenance

### Monitoring

- **Health Endpoints**: Automated monitoring integration
- **Metrics**: Performance counters and custom metrics
- **Alerts**: Circuit breaker state changes, error rates
- **Dashboards**: Request volume, latency, error rates

### Troubleshooting

- **Logs**: Centralized logging with correlation IDs
- **Health Checks**: Detailed component status
- **Circuit Breaker**: Automatic failure isolation
- **Timeouts**: Clear timeout boundaries

For questions or issues, refer to the structured logging output and health check endpoints for diagnostic information.
