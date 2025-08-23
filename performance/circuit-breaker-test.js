import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";
import { bff } from "./config.js";

// Custom metrics for circuit breaker testing
const errorRate = new Rate("error_rate");
const responseTime = new Trend("response_time");
const circuitBreakerTriggered = new Counter("circuit_breaker_triggered");
const successAfterRecovery = new Counter("success_after_recovery");
const failureCount = new Counter("failure_count");

// Circuit Breaker Test Configuration
export const options = {
  scenarios: {
    // Phase 1: Generate failures to trigger circuit breaker
    trigger_circuit_breaker: {
      executor: "constant-arrival-rate",
      rate: 10, // 10 requests per second
      timeUnit: "1s",
      duration: "15s", // Generate failures for 15 seconds
      preAllocatedVUs: 5,
      maxVUs: 20,
      tags: { phase: "trigger" },
      exec: "triggerFailures",
    },
    // Phase 2: Test circuit breaker behavior (should fail fast)
    test_circuit_open: {
      executor: "constant-arrival-rate",
      rate: 5, // 5 requests per second during circuit open
      timeUnit: "1s",
      duration: "35s", // Test during circuit breaker open period
      startTime: "16s", // Start after trigger phase
      preAllocatedVUs: 3,
      maxVUs: 10,
      tags: { phase: "circuit_open" },
      exec: "testCircuitOpen",
    },
    // Phase 3: Test recovery after circuit breaker closes
    test_recovery: {
      executor: "constant-arrival-rate",
      rate: 8, // 8 requests per second during recovery
      timeUnit: "1s",
      duration: "20s", // Test recovery phase
      startTime: "52s", // Start after circuit should be closed
      preAllocatedVUs: 4,
      maxVUs: 15,
      tags: { phase: "recovery" },
      exec: "testRecovery",
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<5000"], // More lenient during circuit breaker testing
    error_rate: ["rate<0.8"], // Allow high error rate since we're testing failures
    "http_req_duration{phase:recovery}": ["p(95)<200"], // Recovery should be fast
    "http_req_failed{phase:recovery}": ["rate<0.1"], // Recovery should have low failure rate
  },
};

// Test data
const testUser = { username: "circuitbreakertest", password: "testpass123" };
const testPayment = {
  amount: 100.0,
  currency: "USD",
  destinationAccount: "CB_TEST_ACC",
};

export function setup() {
  console.log("=== BFF Gateway Circuit Breaker Test Setup ===");
  console.log(`BFF Gateway Circuit Breaker Config from appsettings.json:`);
  console.log(`- Failure Threshold: 5 failures`);
  console.log(`- Break Duration: 30 seconds`);
  console.log(`- Sampling Duration: 60 seconds`);
  console.log(`- Testing BFF Gateway endpoints only`);

  // Health check BFF Gateway only
  const healthResponse = http.get(`${bff.baseUrl}/health/ready`);
  check(healthResponse, {
    "Setup: BFF Gateway health check passed": (r) => r.status === 200,
  });

  console.log("=== BFF Gateway Test Plan ===");
  console.log(
    "Phase 1 (0-15s): Generate failures through BFF Gateway to trigger circuit breaker"
  );
  console.log(
    "Phase 2 (16-51s): Test BFF Gateway circuit breaker open behavior"
  );
  console.log(
    "Phase 3 (52-72s): Test BFF Gateway recovery after circuit breaker closes"
  );

  return { startTime: Date.now() };
}

// Phase 1: Trigger circuit breaker by generating failures
export function triggerFailures() {
  const scenario = "Fail"; // Use Fail scenario to generate 500 errors

  // Test both auth and payment endpoints to trigger failures
  testAuthWithScenario(testUser, scenario, "trigger");
  testPaymentWithScenario(testPayment, scenario, "trigger");

  sleep(0.1); // Small delay between requests
}

// Phase 2: Test behavior when circuit breaker is open
export function testCircuitOpen() {
  const scenario = "None"; // Use normal scenario - should fail fast due to circuit breaker

  // These should fail fast due to circuit breaker
  testAuthWithScenario(testUser, scenario, "circuit_open");
  testPaymentWithScenario(testPayment, scenario, "circuit_open");

  sleep(0.2);
}

// Phase 3: Test recovery after circuit breaker closes
export function testRecovery() {
  const scenario = "None"; // Normal requests - should succeed after recovery

  testAuthWithScenario(testUser, scenario, "recovery");
  testPaymentWithScenario(testPayment, scenario, "recovery");

  sleep(0.1);
}

function testAuthWithScenario(user, scenario, phase) {
  const payload = JSON.stringify({
    username: user.username,
    password: user.password,
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
    },
    tags: {
      endpoint: "auth",
      scenario: scenario,
      phase: phase,
    },
  };

  const url = `${bff.baseUrl}/v2/auth/login?scenario=${scenario}`;
  const response = http.post(url, payload, params);

  // Check response characteristics
  const isSuccess = response.status === 200;
  const isFastFailure = response.timings.duration < 100; // Circuit breaker should fail fast
  const isSlowFailure = response.timings.duration > 1000; // Backend failures are slower

  // Log circuit breaker detection
  if (phase === "circuit_open" && isFastFailure && !isSuccess) {
    circuitBreakerTriggered.add(1);
    console.log(
      `Circuit breaker detected: Fast failure (${response.timings.duration}ms) with status ${response.status}`
    );
  }

  if (phase === "recovery" && isSuccess) {
    successAfterRecovery.add(1);
  }

  if (!isSuccess) {
    failureCount.add(1);
  }

  const checks = check(response, {
    [`${phase}: Auth response received`]: (r) => r.status !== 0,
    [`${phase}: Auth - Circuit breaker fast fail (if open)`]: (r) =>
      phase !== "circuit_open" || r.timings.duration < 100,
    [`${phase}: Auth - Success during recovery`]: (r) =>
      phase !== "recovery" || r.status === 200,
  });

  errorRate.add(!isSuccess);
  responseTime.add(response.timings.duration);

  return response;
}

function testPaymentWithScenario(payment, scenario, phase) {
  const payload = JSON.stringify({
    amount: payment.amount,
    currency: payment.currency,
    destinationAccount: payment.destinationAccount,
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
    },
    tags: {
      endpoint: "payment",
      scenario: scenario,
      phase: phase,
    },
  };

  const url = `${bff.baseUrl}/v2/payments?scenario=${scenario}`;
  const response = http.post(url, payload, params);

  // Check response characteristics
  const isSuccess = response.status === 200;
  const isFastFailure = response.timings.duration < 100;

  // Log circuit breaker detection
  if (phase === "circuit_open" && isFastFailure && !isSuccess) {
    circuitBreakerTriggered.add(1);
    console.log(
      `Circuit breaker detected: Fast failure (${response.timings.duration}ms) with status ${response.status}`
    );
  }

  if (phase === "recovery" && isSuccess) {
    successAfterRecovery.add(1);
  }

  if (!isSuccess) {
    failureCount.add(1);
  }

  const checks = check(response, {
    [`${phase}: Payment response received`]: (r) => r.status !== 0,
    [`${phase}: Payment - Circuit breaker fast fail (if open)`]: (r) =>
      phase !== "circuit_open" || r.timings.duration < 100,
    [`${phase}: Payment - Success during recovery`]: (r) =>
      phase !== "recovery" || r.status === 200,
  });

  errorRate.add(!isSuccess);
  responseTime.add(response.timings.duration);

  return response;
}

export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;

  console.log("=== Circuit Breaker Test Results ===");
  console.log(`Total test duration: ${duration.toFixed(1)}s`);
  console.log(
    `Circuit breaker triggers detected: ${circuitBreakerTriggered.value || 0}`
  );
  console.log(
    `Successful requests after recovery: ${successAfterRecovery.value || 0}`
  );
  console.log(`Total failures generated: ${failureCount.value || 0}`);

  // Final health check
  const healthResponse = http.get(`${bff.baseUrl}/health/ready`);
  check(healthResponse, {
    "Teardown: Final health check passed": (r) => r.status === 200,
  });

  console.log("=== BFF Gateway Test Analysis ===");
  console.log("Look for these patterns in the BFF Gateway behavior:");
  console.log(
    "1. Phase 1: High failure rate with slower response times (500ms+) from BFF Gateway"
  );
  console.log(
    "2. Phase 2: Fast failures (<100ms) indicating BFF Gateway circuit breaker is open"
  );
  console.log("3. Phase 3: Return to normal response times and success rates");
  console.log("\n📊 Monitor BFF Gateway logs for circuit breaker events:");
  console.log("- 'Circuit breaker OPEN'");
  console.log("- 'Circuit breaker HALF-OPEN'");
  console.log("- 'Circuit breaker RESET'");
  console.log(
    "\n🎯 This test focuses on BFF Gateway circuit breaker behavior only"
  );
}
