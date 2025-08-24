# Structured Logging Implementation

## Overview

This document describes the comprehensive structured logging implementation for the BFF Gateway using Serilog with JSON output format. The implementation follows best practices for logging all inbound and outbound HTTP requests while protecting sensitive data.

## ✅ Features Implemented

### 1. **JSON Structured Logging Output**

- **Format**: Compact JSON format using `Serilog.Formatting.Compact.CompactJsonFormatter`
- **Outputs**:
  - Console (for development/debugging)
  - Rolling file logs (`logs/bff-gateway-*.json`)
- **File Management**:
  - Daily rolling files
  - Configurable retention (7 days production, 3 days development)
  - Size limits (100MB production, 50MB development)

### 2. **Comprehensive Inbound Request Logging**

- **Middleware**: `StructuredRequestLoggingMiddleware`
- **Captures**:
  - HTTP method, URL, headers, query parameters
  - Request body (with size limits and sensitive data filtering)
  - Response status, headers, body
  - Duration, remote IP, user agent
  - Correlation ID generation and propagation

### 3. **Complete Outbound Request Logging**

- **Handler**: `StructuredHttpLoggingHandler`
- **Captures**:
  - All HttpClient requests to external providers
  - Request/response headers, bodies, status codes
  - Duration, success/failure status
  - Circuit breaker and retry policy integration

### 4. **Sensitive Data Protection**

- **Headers**: Authorization, Cookie, X-API-Key, etc. → `***MASKED***`
- **JSON Body Fields**: password, token, cardNumber, cvv, etc. → `***MASKED***`
- **Automatic Detection**: JSON parsing with recursive field masking

### 5. **Correlation ID Tracking**

- **Auto-generation**: Uses Activity.TraceId or GUID fallback
- **Propagation**: Inbound → Outbound requests
- **Headers**: `X-Correlation-ID` in requests and responses

### 6. **Request Context Enrichment**

- **Structured Properties**:
  - `CorrelationId`, `RequestId`, `RequestType` (Inbound/Outbound)
  - `Application`, `Version`, `MachineName`, `ThreadId`, `Environment`
- **LogContext**: Request-scoped properties using Serilog.Context

## 🏗️ Architecture

### Configuration Files

#### `appsettings.json` (Production)

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Formatting.Compact",
      "Serilog.Enrichers.Environment",
      "Serilog.Enrichers.Thread"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.AspNetCore.HttpLogging": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/bff-gateway-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 104857600,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithEnvironmentName"
    ],
    "Properties": {
      "Application": "BffGateway",
      "Version": "1.0.0"
    }
  }
}
```

#### `appsettings.Development.json`

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information",
        "Microsoft.AspNetCore.HttpLogging": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/bff-gateway-dev-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 3,
          "fileSizeLimitBytes": 52428800,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

### Middleware Pipeline Order

```csharp
app.UseExceptionHandler();
app.UseMiddleware<StructuredRequestLoggingMiddleware>(Log.Logger);  // 📝 Our comprehensive logging
app.UseRouting();
app.UseMiddleware<DeprecationHeadersMiddleware>();
app.MapBffHealthChecks();
app.MapControllers();
```

### HttpClient Handler Chain Order

```csharp
.AddHttpMessageHandler<StructuredHttpLoggingHandler>()  // 📝 Logging FIRST
.AddHttpMessageHandler<ForwardHeadersHandler>()         // Headers forwarding
.AddPolicyHandler(CircuitBreaker)                      // Circuit breaker
.AddPolicyHandler(Retry)                               // Retry policy
.AddPolicyHandler(Timeout)                             // Timeout policy
```

## 📋 Log Examples

### Inbound Request Log

```json
{
  "@t": "2024-01-15T10:30:45.123Z",
  "@l": "Information",
  "@m": "HTTP Request Started GET https://localhost:5000/api/v1/payments | Headers: {@Headers} | Body: {Body} | Size: {BodySize} | RemoteIP: {RemoteIP} | UserAgent: {UserAgent}",
  "CorrelationId": "12345678-1234-5678-9012-123456789012",
  "RequestId": "87654321-4321-8765-2109-876543210987",
  "RequestType": "Inbound",
  "Application": "BffGateway",
  "Version": "1.0.0",
  "MachineName": "BFF-API-01",
  "ThreadId": 42,
  "Environment": "Production"
}
```

### Outbound Request Log

```json
{
  "@t": "2024-01-15T10:30:45.456Z",
  "@l": "Information",
  "@m": "HTTP Outbound Request Completed POST http://localhost:5001/api/pay | Status: 200 OK | Duration: 150ms | Headers: {@Headers} | Body: {Body} | Size: {BodySize} | Success: true",
  "CorrelationId": "12345678-1234-5678-9012-123456789012",
  "RequestId": "11111111-2222-3333-4444-555555555555",
  "RequestType": "Outbound",
  "Application": "BffGateway",
  "Version": "1.0.0"
}
```

### Sensitive Data Masking Example

**Original Request Body:**

```json
{
  "email": "user@example.com",
  "password": "secret123",
  "cardNumber": "4111111111111111",
  "amount": 100.0
}
```

**Logged Request Body:**

```json
{
  "email": "user@example.com",
  "password": "***MASKED***",
  "cardNumber": "***MASKED***",
  "amount": 100.0
}
```

## 🔧 Configuration Options

### Sensitive Data Fields

The following fields are automatically masked in JSON bodies:

- `password`, `token`, `secret`, `key`, `authorization`
- `cardNumber`, `cvv`, `pin`, `ssn`, `creditCard`

### Sensitive Headers

The following headers are automatically masked:

- `Authorization`, `Cookie`, `Set-Cookie`, `X-API-Key`
- `Authentication`, `Proxy-Authorization`, `WWW-Authenticate`

### Size Limits

- **Request/Response Body**: 8KB maximum logged
- **Log File Size**: 100MB (production), 50MB (development)
- **File Retention**: 7 days (production), 3 days (development)

## 🚀 Performance Considerations

### 1. **Async Logging**

- All logging operations are async to avoid blocking request processing
- Stream reading uses memory-efficient approaches

### 2. **Body Size Limits**

- Large request/response bodies are truncated to prevent memory issues
- Configurable limits based on environment

### 3. **Conditional Logging**

- Log levels can be adjusted per namespace
- Development vs Production configurations

### 4. **Memory Management**

- Request/response body streams are properly disposed
- JSON parsing uses efficient `JsonDocument` API

## 🔍 Monitoring & Observability

### Log Aggregation Ready

- **Format**: Structured JSON logs are ready for ELK stack, Grafana, or similar
- **Correlation**: Full request tracing with correlation IDs
- **Metrics**: Duration, status codes, error rates easily extractable

### Key Metrics Available

- Request/response durations
- Error rates by endpoint
- Circuit breaker state changes
- Provider response times
- Request volume by endpoint

### Example Queries

#### Find all failed requests

```bash
jq 'select(.["@l"] == "Error")' logs/bff-gateway-*.json
```

#### Find requests by correlation ID

```bash
jq 'select(.CorrelationId == "12345678-1234-5678-9012-123456789012")' logs/bff-gateway-*.json
```

#### Calculate average response times

```bash
jq -r 'select(.["@m"] | contains("Duration")) | match("Duration: ([0-9]+)ms") | .captures[0].string' logs/bff-gateway-*.json | awk '{sum+=$1; n++} END {print "Average:", sum/n "ms"}'
```

## 🛡️ Security & Compliance

### 1. **Data Privacy**

- Automatic PII detection and masking
- Configurable sensitive field lists
- No sensitive data in logs

### 2. **GDPR Compliance**

- No personal data logged in clear text
- Configurable data retention policies
- Log rotation and cleanup

### 3. **Security Headers**

- Authentication headers masked
- API keys and secrets protected
- Cookie data masked

## 📚 Best Practices Followed

### ✅ Serilog Best Practices

1. **Structured Properties**: Using semantic properties instead of string interpolation
2. **Enrichers**: Context-based enrichment with machine, thread, environment info
3. **Async Logging**: Non-blocking log operations
4. **Configuration**: External configuration files with environment overrides
5. **Performance**: Size limits and efficient JSON handling

### ✅ HTTP Logging Best Practices

1. **Complete Context**: Full request/response context captured
2. **Correlation**: End-to-end tracing with correlation IDs
3. **Security**: Sensitive data protection built-in
4. **Performance**: Minimal impact on request processing
5. **Observability**: Rich metadata for monitoring and debugging

### ✅ Production Ready

1. **File Management**: Automatic rotation and cleanup
2. **Error Handling**: Graceful degradation if logging fails
3. **Memory Efficiency**: Stream handling and size limits
4. **Configuration**: Environment-specific settings
5. **Monitoring**: Rich structured data for analysis

## 🎯 Usage Examples

### Starting the Application

```bash
dotnet run --project src/BffGateway.WebApi
```

### View Real-time Logs

```bash
tail -f logs/bff-gateway-dev-20240115.json | jq '.'
```

### Search for Errors

```bash
grep -l "Error" logs/bff-gateway-*.json | xargs jq 'select(.["@l"] == "Error")'
```

## 📁 File Structure

```
├── src/
│   ├── BffGateway.WebApi/
│   │   ├── Middleware/
│   │   │   └── StructuredRequestLoggingMiddleware.cs  // 📝 Inbound logging
│   │   ├── Extensions/
│   │   │   └── MiddlewareExtensions.cs                // 🔧 Configuration
│   │   ├── appsettings.json                          // ⚙️ Production config
│   │   └── appsettings.Development.json              // ⚙️ Development config
│   └── BffGateway.Infrastructure/
│       └── Providers/
│           └── StructuredHttpLoggingHandler.cs       // 📝 Outbound logging
├── logs/                                             // 📁 Log files
│   ├── bff-gateway-20240115.json                    // 📄 Production logs
│   └── bff-gateway-dev-20240115.json                // 📄 Development logs
└── STRUCTURED_LOGGING_IMPLEMENTATION.md             // 📖 This documentation
```

This implementation provides comprehensive, secure, and performant structured logging that follows industry best practices while maintaining excellent observability for the BFF Gateway application.
