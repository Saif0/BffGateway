# Circuit Breaker Testing Guide

This guide explains how to test the Circuit Breaker pattern implementation in the BFF Gateway using K6 and simulation scenarios.

## Overview

The BFF Gateway implements circuit breaker patterns to prevent cascading failures when the MockProvider becomes unhealthy. By using simulation scenarios, we can intentionally trigger failures to test circuit breaker behavior.

## Circuit Breaker Configuration

Current configuration in `src/BffGateway.WebApi/appsettings.json`:

```json
{
  "CircuitBreaker": {
    "FailureThreshold": 5, // Open after 5 failures
    "SamplingDurationSeconds": 60, // Within 60 seconds
    "MinimumThroughput": 10, // Minimum requests to evaluate
    "DurationOfBreakSeconds": 30 // Stay open for 30 seconds
  }
}
```

## Simulation Scenarios

| Scenario        | Effect                | HTTP Status | Use Case                |
| --------------- | --------------------- | ----------- | ----------------------- |
| `None`          | Normal operation      | 200         | Baseline testing        |
| `Fail`          | Server error          | 500         | Trigger circuit breaker |
| `Timeout`       | Slow response/timeout | 0 or slow   | Test timeout handling   |
| `LimitExceeded` | Rate limit error      | 429         | Test rate limiting      |

## Test Files

### 1. `validate-circuit-breaker-setup.js`

**Purpose**: Validate that all scenarios work correctly before running circuit breaker tests.

```bash
k6 run performance/validate-circuit-breaker-setup.js
```

**What it tests**:

- Service health checks
- All simulation scenarios work as expected
- Response time baselines

### 2. `circuit-breaker-test.js`

**Purpose**: Complete circuit breaker lifecycle test with three phases.

```bash
k6 run performance/circuit-breaker-test.js
# OR
./scripts/test-circuit-breaker.sh
```

**Test phases**:

1. **Trigger (0-15s)**: Generate failures using `Fail` scenario
2. **Circuit Open (16-51s)**: Test fast failures when circuit breaker is open
3. **Recovery (52-72s)**: Test normal operation after circuit breaker resets

### 3. `circuit-breaker-scenarios.js`

**Purpose**: Test different scenarios simultaneously to trigger circuit breaker.

```bash
k6 run performance/circuit-breaker-scenarios.js
```

**What it tests**:

- Multiple scenarios running in parallel
- Different types of failures
- Circuit breaker behavior with mixed workloads

## Running the Tests

### Prerequisites

1. **Install K6**:

   ```bash
   brew install k6  # macOS
   # OR visit https://k6.io/docs/get-started/installation/
   ```

2. **Start Services**:

   ```bash
   # Terminal 1: Start BFF Gateway
   cd src/BffGateway.WebApi
   dotnet run --urls http://localhost:5000

   # Terminal 2: Start MockProvider
   cd src/MockProvider
   dotnet run --urls http://localhost:5002
   ```

### Step-by-Step Testing

1. **Validate Setup**:

   ```bash
   k6 run performance/validate-circuit-breaker-setup.js
   ```

2. **Run Circuit Breaker Test**:

   ```bash
   ./scripts/test-circuit-breaker.sh
   ```

3. **Monitor Logs**: Watch BFF Gateway console for circuit breaker events:
   - `Circuit breaker OPEN for outbound provider calls`
   - `Circuit breaker HALF-OPEN for outbound provider calls`
   - `Circuit breaker RESET for outbound provider calls`

## Expected Behavior

### Phase 1: Triggering Circuit Breaker

- **Requests**: Using `Fail` scenario
- **Expected**: High error rate (500 status), slower response times
- **Goal**: Generate 5+ failures to trigger circuit breaker

### Phase 2: Circuit Breaker Open

- **Requests**: Normal requests (`None` scenario)
- **Expected**: Fast failures (<100ms), 503 status codes
- **Behavior**: Circuit breaker blocks requests without hitting backend

### Phase 3: Recovery

- **Requests**: Normal requests (`None` scenario)
- **Expected**: Return to normal response times and success rates
- **Behavior**: Circuit breaker allows traffic through again

## Analyzing Results

### K6 Output Metrics

- `error_rate`: Overall failure percentage
- `circuit_breaker_triggered`: Number of detected circuit breaker activations
- `success_after_recovery`: Successful requests after circuit reset
- `http_req_duration`: Response time trends

### Log Analysis

Look for these patterns in BFF Gateway logs:

```
[Warning] Circuit breaker OPEN for outbound provider calls for 30s due to HttpRequestException
[Information] Circuit breaker HALF-OPEN for outbound provider calls
[Information] Circuit breaker RESET for outbound provider calls
```

### Response Time Patterns

1. **Normal**: 50-150ms
2. **Backend Failures**: 500-2000ms (actual backend processing)
3. **Circuit Breaker Open**: <100ms (fast failure)

## Customizing Tests

### Adjust Circuit Breaker Settings

Edit `src/BffGateway.WebApi/appsettings.json`:

```json
{
  "Provider": {
    "CircuitBreaker": {
      "FailureThreshold": 3, // Trigger faster
      "DurationOfBreakSeconds": 15 // Shorter break period
    }
  }
}
```

### Modify Test Scenarios

Edit the K6 test files to:

- Change request rates (`rate` parameter)
- Adjust test duration
- Test different endpoints
- Use different simulation scenarios

### Create Custom Scenarios

Add new scenarios to the MockProvider and use them in tests:

```javascript
// In K6 test
const customScenario = "YourNewScenario";
const url = `${bff.baseUrl}/v2/auth/login?scenario=${customScenario}`;
```

## Troubleshooting

### Services Not Responding

- Check that both BFF Gateway and MockProvider are running
- Verify ports: BFF on 5000, MockProvider on 5002
- Check health endpoints: `/health/ready`

### Circuit Breaker Not Triggering

- Increase failure rate in test
- Verify FailureThreshold setting
- Check that scenarios are generating actual failures (500 status)

### Tests Timing Out

- Increase timeout values in K6 options
- Check MockProvider timeout settings
- Verify network connectivity

## Advanced Testing

### Load Testing with Circuit Breaker

Combine circuit breaker tests with high load:

```bash
k6 run --vus 50 --duration 2m performance/circuit-breaker-test.js
```

### Monitoring Integration

Integrate with monitoring tools:

- Grafana dashboards for circuit breaker metrics
- Prometheus metrics collection
- Alert rules for circuit breaker state changes

### Production Testing

For production environments:

- Use lower request rates
- Longer observation periods
- Coordinate with infrastructure team
- Have rollback plan ready
