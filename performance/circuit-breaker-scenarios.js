import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";
import { bff } from "./config.js";

// Custom metrics
const errorRate = new Rate("error_rate");
const responseTime = new Trend("response_time");
const scenarioMetrics = {
  fail: new Counter("fail_scenario_count"),
  timeout: new Counter("timeout_scenario_count"),
  limitExceeded: new Counter("limit_exceeded_scenario_count"),
  none: new Counter("none_scenario_count"),
};

// Test different scenarios to trigger various circuit breaker conditions
export const options = {
  scenarios: {
    // Test with Fail scenario - should cause 500 errors
    test_fail_scenario: {
      executor: "constant-arrival-rate",
      rate: 3,
      timeUnit: "1s",
      duration: "20s",
      preAllocatedVUs: 2,
      maxVUs: 5,
      tags: { test_scenario: "fail" },
      exec: "testFailScenario",
    },
    // Test with Timeout scenario - should cause timeouts
    test_timeout_scenario: {
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: "25s",
      startTime: "5s",
      preAllocatedVUs: 2,
      maxVUs: 5,
      tags: { test_scenario: "timeout" },
      exec: "testTimeoutScenario",
    },
    // Test with LimitExceeded scenario - should cause 429 errors
    test_limit_scenario: {
      executor: "constant-arrival-rate",
      rate: 4,
      timeUnit: "1s",
      duration: "15s",
      startTime: "10s",
      preAllocatedVUs: 2,
      maxVUs: 5,
      tags: { test_scenario: "limit_exceeded" },
      exec: "testLimitExceededScenario",
    },
    // Test normal requests during circuit breaker activation
    test_normal_during_cb: {
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: "30s",
      startTime: "15s",
      preAllocatedVUs: 2,
      maxVUs: 5,
      tags: { test_scenario: "normal_during_cb" },
      exec: "testNormalScenario",
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<10000"], // Allow for timeouts
    error_rate: ["rate<0.9"], // High error rate expected during CB testing
    "http_req_duration{test_scenario:normal_during_cb}": ["p(95)<500"], // Normal requests should be fast
  },
};

const testData = {
  user: { username: "cbtest", password: "cbpass123" },
  payment: { amount: 150.0, currency: "USD", destinationAccount: "CB_TEST" },
};

export function setup() {
  console.log("=== BFF Gateway Circuit Breaker Scenarios Test ===");
  console.log(
    "Testing different simulation scenarios through BFF Gateway to trigger circuit breaker"
  );

  const healthResponse = http.get(`${bff.baseUrl}/health/ready`);
  check(healthResponse, {
    "BFF Gateway health check passed": (r) => r.status === 200,
  });

  console.log("🎯 Focus: BFF Gateway circuit breaker behavior only");
  return { startTime: Date.now() };
}

export function testFailScenario() {
  testWithScenario("Fail", "fail");
  sleep(0.3);
}

export function testTimeoutScenario() {
  testWithScenario("Timeout", "timeout");
  sleep(0.5); // Longer sleep for timeout tests
}

export function testLimitExceededScenario() {
  testWithScenario("LimitExceeded", "limitExceeded");
  sleep(0.2);
}

export function testNormalScenario() {
  testWithScenario("None", "none");
  sleep(0.4);
}

function testWithScenario(scenario, metricKey) {
  // Test auth endpoint
  const authResult = callAuthEndpoint(scenario);

  // Test payment endpoint
  const paymentResult = callPaymentEndpoint(scenario);

  // Update scenario-specific metrics
  if (scenarioMetrics[metricKey]) {
    scenarioMetrics[metricKey].add(1);
  }

  // Analyze response patterns
  analyzeCircuitBreakerBehavior(authResult, paymentResult, scenario);
}

function callAuthEndpoint(scenario) {
  const payload = JSON.stringify({
    username: testData.user.username,
    password: testData.user.password,
  });

  const params = {
    headers: { "Content-Type": "application/json" },
    tags: { endpoint: "auth", scenario: scenario },
    timeout: "8s", // Allow for timeout scenarios
  };

  const url = `${bff.baseUrl}/v2/auth/login?scenario=${scenario}`;
  const response = http.post(url, payload, params);

  const isSuccess = response.status === 200;
  const isFastFailure = response.timings.duration < 200;
  const isTimeout = response.status === 0 || response.timings.duration > 5000;

  check(response, {
    [`Auth ${scenario}: Response received`]: (r) =>
      r.status !== 0 || scenario === "Timeout",
    [`Auth ${scenario}: Expected behavior`]: (r) =>
      validateExpectedBehavior(r, scenario),
  });

  errorRate.add(!isSuccess);
  responseTime.add(response.timings.duration);

  return {
    status: response.status,
    duration: response.timings.duration,
    success: isSuccess,
    fastFailure: isFastFailure,
    timeout: isTimeout,
    scenario: scenario,
  };
}

function callPaymentEndpoint(scenario) {
  const payload = JSON.stringify({
    amount: testData.payment.amount,
    currency: testData.payment.currency,
    destinationAccount: testData.payment.destinationAccount,
  });

  const params = {
    headers: { "Content-Type": "application/json" },
    tags: { endpoint: "payment", scenario: scenario },
    timeout: "8s",
  };

  const url = `${bff.baseUrl}/v2/payments?scenario=${scenario}`;
  const response = http.post(url, payload, params);

  const isSuccess = response.status === 200;
  const isFastFailure = response.timings.duration < 200;
  const isTimeout = response.status === 0 || response.timings.duration > 5000;

  check(response, {
    [`Payment ${scenario}: Response received`]: (r) =>
      r.status !== 0 || scenario === "Timeout",
    [`Payment ${scenario}: Expected behavior`]: (r) =>
      validateExpectedBehavior(r, scenario),
  });

  errorRate.add(!isSuccess);
  responseTime.add(response.timings.duration);

  return {
    status: response.status,
    duration: response.timings.duration,
    success: isSuccess,
    fastFailure: isFastFailure,
    timeout: isTimeout,
    scenario: scenario,
  };
}

function validateExpectedBehavior(response, scenario) {
  switch (scenario) {
    case "Fail":
      return response.status === 500 || response.status === 503; // Circuit breaker may cause 503
    case "Timeout":
      return response.status === 0 || response.timings.duration > 4000; // Timeout or very slow
    case "LimitExceeded":
      return response.status === 429 || response.status === 503; // Rate limit or circuit breaker
    case "None":
      return response.status === 200 || response.status === 503; // Success or circuit breaker
    default:
      return true;
  }
}

function analyzeCircuitBreakerBehavior(authResult, paymentResult, scenario) {
  const results = [authResult, paymentResult];

  // Detect potential circuit breaker activation
  const hasFastFailures = results.some((r) => r.fastFailure && !r.success);
  const hasSlowFailures = results.some((r) => r.duration > 1000 && !r.success);

  if (hasFastFailures) {
    console.log(
      `🔴 Potential circuit breaker detected with ${scenario} scenario - fast failures`
    );
  }

  if (
    scenario === "None" &&
    results.every((r) => r.fastFailure && !r.success)
  ) {
    console.log(
      `🟠 Circuit breaker likely OPEN - normal requests failing fast`
    );
  }

  if (scenario !== "None" && hasSlowFailures) {
    console.log(
      `🟡 Backend failures detected with ${scenario} scenario - may trigger circuit breaker`
    );
  }
}

export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;

  console.log("=== BFF Gateway Circuit Breaker Scenarios Test Results ===");
  console.log(`Test duration: ${duration.toFixed(1)}s`);
  console.log(`Fail scenarios tested: ${scenarioMetrics.fail.value || 0}`);
  console.log(
    `Timeout scenarios tested: ${scenarioMetrics.timeout.value || 0}`
  );
  console.log(
    `LimitExceeded scenarios tested: ${
      scenarioMetrics.limitExceeded.value || 0
    }`
  );
  console.log(`Normal scenarios tested: ${scenarioMetrics.none.value || 0}`);

  console.log("\n=== Expected BFF Gateway Circuit Breaker Behavior ===");
  console.log(
    "1. 'Fail' scenario: BFF Gateway should receive 500 errors from backend"
  );
  console.log(
    "2. 'Timeout' scenario: BFF Gateway should handle request timeouts"
  );
  console.log(
    "3. 'LimitExceeded' scenario: BFF Gateway should receive 429 errors"
  );
  console.log("4. After 5+ failures: BFF Gateway circuit breaker should OPEN");
  console.log(
    "5. During OPEN: BFF Gateway fast failures (< 200ms) for all requests"
  );
  console.log(
    "6. After 30s: BFF Gateway circuit breaker should attempt to RESET"
  );

  console.log("\n🔍 Check BFF Gateway logs for circuit breaker state changes!");
  console.log(
    "🎯 This test focuses on BFF Gateway circuit breaker behavior only"
  );
}
