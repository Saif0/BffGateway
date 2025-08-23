import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Counter } from "k6/metrics";
import { bff } from "./config.js";

// Simple circuit breaker test
const errorRate = new Rate("error_rate");
const circuitBreakerCount = new Counter("circuit_breaker_activations");

export const options = {
  scenarios: {
    trigger_failures: {
      executor: "constant-arrival-rate",
      rate: 5, // 5 requests per second
      timeUnit: "1s",
      duration: "10s", // Generate failures for 10 seconds
      preAllocatedVUs: 3,
      maxVUs: 10,
      exec: "generateFailures",
    },
    test_circuit_open: {
      executor: "constant-arrival-rate",
      rate: 3, // 3 requests per second during circuit open
      timeUnit: "1s",
      duration: "35s", // Test during circuit breaker period
      startTime: "11s", // Start after trigger phase
      preAllocatedVUs: 2,
      maxVUs: 5,
      exec: "testCircuitOpen",
    },
  },
};

export function setup() {
  console.log("🔥 Quick BFF Gateway Circuit Breaker Test");
  console.log(
    "Generating failures through BFF Gateway to trigger circuit breaker..."
  );
  console.log("🎯 Testing BFF Gateway circuit breaker behavior only");

  const health = http.get(`${bff.baseUrl}/health/ready`);
  check(health, { "BFF Gateway healthy": (r) => r.status === 200 });

  return { startTime: Date.now() };
}

export function generateFailures() {
  const response = http.post(
    `${bff.baseUrl}/v2/auth/login?scenario=Fail`,
    JSON.stringify({ username: "test", password: "test" }),
    { headers: { "Content-Type": "application/json" } }
  );

  const isError = response.status >= 400;
  errorRate.add(isError);

  console.log(
    `Failure generation: ${response.status} in ${response.timings.duration}ms`
  );

  check(response, {
    "Failure generated": (r) => r.status >= 400,
  });

  sleep(0.1);
}

export function testCircuitOpen() {
  const response = http.post(
    `${bff.baseUrl}/v2/auth/login?scenario=None`,
    JSON.stringify({ username: "test", password: "test" }),
    { headers: { "Content-Type": "application/json" } }
  );

  const isError = response.status >= 400;
  const isFastFailure = response.timings.duration < 200;

  errorRate.add(isError);

  if (isError && isFastFailure) {
    circuitBreakerCount.add(1);
    console.log(
      `🔴 CIRCUIT BREAKER DETECTED: ${response.status} in ${response.timings.duration}ms`
    );
  }

  console.log(
    `Circuit test: ${response.status} in ${response.timings.duration}ms`
  );

  check(response, {
    "Response received": (r) => r.status !== 0,
  });

  sleep(0.2);
}

export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`\n🎯 BFF Gateway test completed in ${duration}s`);
  console.log(
    `⚡ BFF Gateway circuit breaker activations detected: ${
      circuitBreakerCount.value || 0
    }`
  );
  console.log(`📊 Overall error rate: ${(errorRate.rate * 100).toFixed(1)}%`);

  console.log("\n🔍 Check your BFF Gateway logs for circuit breaker events:");
  console.log("   'Circuit breaker OPEN'");
  console.log("   'Circuit breaker RESET'");
  console.log(
    "🎯 This test focuses on BFF Gateway circuit breaker behavior only"
  );
}
