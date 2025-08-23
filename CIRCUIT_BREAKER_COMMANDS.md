# BFF Gateway Circuit Breaker Testing Commands

This document lists all the available Make commands for testing the circuit breaker functionality in the BFF Gateway. **All tests focus exclusively on BFF Gateway behavior** - no direct provider testing is performed.

## Quick Reference

```bash
# Validate setup first
make circuit-breaker-validate

# Run quick test
make circuit-breaker-quick

# Run complete test
make circuit-breaker

# Test multiple scenarios
make circuit-breaker-scenarios

# Run all tests
make circuit-breaker-all

# Full test with service management
make circuit-breaker-full
```

## Command Details

### `make circuit-breaker-validate`

**Purpose**: Validate that the BFF Gateway circuit breaker setup is working correctly.

**What it does**:

- Checks BFF Gateway health
- Tests all simulation scenarios through BFF Gateway (None, Fail, Timeout, LimitExceeded)
- Validates BFF Gateway response patterns and timing
- Confirms scenarios are working as expected through BFF Gateway

**When to use**: Before running any circuit breaker tests to ensure BFF Gateway setup is correct.

**Focus**: BFF Gateway endpoints only

**Example output**:

```
🔍 Validating circuit breaker setup...
✅ Services are running
🧪 Testing all simulation scenarios...
✅ All scenarios are working correctly
```

---

### `make circuit-breaker-quick`

**Purpose**: Run a focused, quick circuit breaker test.

**What it does**:

- Generates failures for 10 seconds
- Tests circuit breaker behavior for 35 seconds
- Fast test to verify circuit breaker triggers

**Duration**: ~45 seconds

**When to use**: Quick verification that circuit breaker is working.

---

### `make circuit-breaker`

**Purpose**: Run the complete 3-phase circuit breaker test.

**What it does**:

- **Phase 1 (0-15s)**: Generate failures using `Fail` scenario
- **Phase 2 (16-51s)**: Test fast failures when circuit breaker is open
- **Phase 3 (52-72s)**: Test recovery when circuit breaker resets

**Duration**: ~72 seconds

**When to use**: Comprehensive testing of complete circuit breaker lifecycle.

**Expected behavior**:

1. Phase 1: Slow failures (~8s response time) due to retries
2. Phase 2: Fast failures (<100ms) when circuit breaker is open
3. Phase 3: Return to normal response times after recovery

---

### `make circuit-breaker-scenarios`

**Purpose**: Test multiple simulation scenarios simultaneously.

**What it does**:

- Runs Fail, Timeout, LimitExceeded, and None scenarios in parallel
- Tests different types of failures
- Analyzes circuit breaker behavior with mixed workloads

**Duration**: ~45 seconds

**When to use**: Testing circuit breaker with various failure types.

---

### `make circuit-breaker-all`

**Purpose**: Run all circuit breaker tests in sequence.

**What it does**:

- Runs `circuit-breaker-validate`
- Runs `circuit-breaker`
- Runs `circuit-breaker-scenarios`

**Duration**: ~3-4 minutes

**When to use**: Complete test suite execution.

---

### `make circuit-breaker-full`

**Purpose**: Full test with service management and guidance.

**What it does**:

- Checks if services are running
- Validates setup
- Runs complete circuit breaker test
- Provides guided experience with clear instructions

**Duration**: Variable (includes service checks)

**When to use**: When you want a guided, complete testing experience.

## Prerequisites

### 1. Install K6

```bash
brew install k6  # macOS
# OR visit https://k6.io/docs/get-started/installation/
```

### 2. Start Services

```bash
# Option 1: Use Make commands
make run-all       # Start both services
make run-gateway   # Start only BFF Gateway
make run-provider  # Start only MockProvider

# Option 2: Manual startup
# Terminal 1: BFF Gateway
cd src/BffGateway.WebApi
dotnet run --urls http://localhost:5000

# Terminal 2: MockProvider
cd src/MockProvider
dotnet run --urls http://localhost:5002
```

### 3. Verify Services

```bash
curl http://localhost:5000/health  # BFF Gateway
curl http://localhost:5002/health  # MockProvider
```

## Understanding the Output

### Success Indicators

- ✅ **Fast failures during circuit open**: Response times < 100ms
- ✅ **Normal response times during recovery**: 50-150ms
- ✅ **Circuit breaker logs**: Watch for "Circuit breaker OPEN/RESET" messages

### Expected Patterns

```
Phase 1: Generate failures
  Status: 400-500, Time: 5000-8000ms (with retries)

Phase 2: Circuit breaker open
  Status: 400-503, Time: <100ms (fast fail)

Phase 3: Recovery
  Status: 200, Time: 50-150ms (normal)
```

### Circuit Breaker Logs

Watch your BFF Gateway console for these messages:

```
[Warning] Circuit breaker OPEN for outbound provider calls for 30s
[Information] Circuit breaker HALF-OPEN for outbound provider calls
[Information] Circuit breaker RESET for outbound provider calls
```

## Troubleshooting

### Services Not Running

```bash
# Check what's running
make circuit-breaker-validate

# Start services if needed
make run-all
```

### Circuit Breaker Not Triggering

- Check failure threshold: Currently set to 5 failures
- Verify scenarios generate actual failures (check logs)
- Increase test duration or failure rate

### Tests Timing Out

- Increase timeout values in test configuration
- Check network connectivity between services
- Verify MockProvider is responding to scenarios

## Configuration

### Circuit Breaker Settings

Edit `src/BffGateway.WebApi/appsettings.json`:

```json
{
  "Provider": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "DurationOfBreakSeconds": 30,
      "SamplingDurationSeconds": 60
    }
  }
}
```

### Test Customization

- Modify K6 test files in `performance/` directory
- Adjust request rates, durations, and scenarios
- Create custom simulation scenarios in MockProvider

## File Locations

```
performance/
├── circuit-breaker-test.js           # Main 3-phase test
├── circuit-breaker-scenarios.js      # Multi-scenario test
├── quick-circuit-breaker-test.js     # Quick focused test
├── validate-circuit-breaker-setup.js # Setup validation
└── CIRCUIT_BREAKER_TESTING.md        # Detailed documentation

scripts/
└── test-circuit-breaker.sh           # Guided test script

Makefile                               # All make commands defined here
```

## Next Steps

After running circuit breaker tests:

1. **Analyze Results**: Look at response time patterns and error rates
2. **Check Logs**: Verify circuit breaker state changes in BFF Gateway logs
3. **Tune Configuration**: Adjust thresholds based on your requirements
4. **Monitor Production**: Set up alerting for circuit breaker events
5. **Document Runbooks**: Create operational procedures for circuit breaker events

## Examples

### Quick Development Test

```bash
make circuit-breaker-validate  # Check setup
make circuit-breaker-quick     # Quick test
```

### Complete Testing

```bash
make circuit-breaker-full      # Guided complete test
```

### Custom Testing

```bash
# Edit thresholds in appsettings.json, then:
make circuit-breaker-all       # Test with new settings
```
