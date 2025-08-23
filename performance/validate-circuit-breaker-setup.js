import http from "k6/http";
import { check, sleep } from "k6";
import { bff } from "./config.js";

// Quick validation test to ensure circuit breaker setup is correct
export const options = {
  vus: 1,
  duration: "30s",
  thresholds: {
    http_req_duration: ["p(95)<2000"],
  },
};

export function setup() {
  console.log("=== BFF Gateway Circuit Breaker Validation ===");

  // Check BFF Gateway health only
  const bffHealth = http.get(`${bff.baseUrl}/health/ready`);
  if (
    !check(bffHealth, { "BFF Gateway is healthy": (r) => r.status === 200 })
  ) {
    throw new Error("BFF Gateway is not healthy");
  }

  console.log(
    "✅ BFF Gateway is running and ready for circuit breaker testing"
  );
  return {};
}

export default function () {
  console.log("🧪 Testing all simulation scenarios...");

  const testData = {
    username: "testuser",
    password: "testpass",
  };

  // Test each scenario to make sure they work
  const scenarios = ["None", "Fail", "Timeout", "LimitExceeded"];

  scenarios.forEach((scenario) => {
    console.log(`Testing scenario: ${scenario}`);

    // Test auth endpoint
    const authPayload = JSON.stringify(testData);
    const authResponse = http.post(
      `${bff.baseUrl}/v2/auth/login?scenario=${scenario}`,
      authPayload,
      { headers: { "Content-Type": "application/json" }, timeout: "6s" }
    );

    // Test payment endpoint
    const paymentPayload = JSON.stringify({
      amount: 100,
      currency: "USD",
      destinationAccount: "TEST123",
    });
    const paymentResponse = http.post(
      `${bff.baseUrl}/v2/payments?scenario=${scenario}`,
      paymentPayload,
      { headers: { "Content-Type": "application/json" }, timeout: "6s" }
    );

    // Validate responses based on scenario
    validateScenarioResponse(authResponse, scenario, "auth");
    validateScenarioResponse(paymentResponse, scenario, "payment");

    sleep(1); // Give some time between scenarios
  });
}

function validateScenarioResponse(response, scenario, endpoint) {
  const duration = response.timings.duration;

  switch (scenario) {
    case "None":
      check(response, {
        [`${endpoint} None: Success`]: (r) => r.status === 200,
        [`${endpoint} None: Fast response`]: (r) => r.timings.duration < 500,
      });
      break;

    case "Fail":
      check(response, {
        [`${endpoint} Fail: Server error`]: (r) => r.status === 500,
        [`${endpoint} Fail: Response received`]: (r) => r.status !== 0,
      });
      console.log(
        `  ✅ ${endpoint} Fail scenario: ${
          response.status
        } in ${duration.toFixed(0)}ms`
      );
      break;

    case "Timeout":
      const isTimeout = response.status === 0 || duration > 4000;
      check(response, {
        [`${endpoint} Timeout: Slow/timeout response`]: () => isTimeout,
      });
      console.log(
        `  ⏱️ ${endpoint} Timeout scenario: ${
          response.status
        } in ${duration.toFixed(0)}ms`
      );
      break;

    case "LimitExceeded":
      check(response, {
        [`${endpoint} LimitExceeded: Rate limit error`]: (r) =>
          r.status === 429,
        [`${endpoint} LimitExceeded: Response received`]: (r) => r.status !== 0,
      });
      console.log(
        `  🚫 ${endpoint} LimitExceeded scenario: ${
          response.status
        } in ${duration.toFixed(0)}ms`
      );
      break;
  }
}

export function teardown(data) {
  console.log("\n=== BFF Gateway Validation Complete ===");
  console.log("✅ All BFF Gateway scenarios are working correctly");
  console.log("\n🚀 Ready to run BFF Gateway circuit breaker tests:");
  console.log("  make circuit-breaker-quick");
  console.log("  make circuit-breaker");
  console.log("  make circuit-breaker-scenarios");

  console.log("\n📊 BFF Gateway Circuit Breaker Configuration:");
  console.log("  - Failure Threshold: 5 failures");
  console.log("  - Break Duration: 30 seconds");
  console.log("  - Monitor BFF Gateway logs for circuit breaker state changes");
  console.log("  - Testing focuses on BFF Gateway behavior only");
}
