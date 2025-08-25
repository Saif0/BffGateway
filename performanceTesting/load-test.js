import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { bff } from "./config.js";

// Custom metrics
const errorRate = new Rate("error_rate");
const responseTime = new Trend("response_time");

// Unified config imported from config.js

// Test configuration
export const options = {
  scenarios: {
    sustained_load: {
      executor: "constant-arrival-rate",
      rate: bff.rps,
      timeUnit: "1s",
      duration: bff.duration,
      preAllocatedVUs: bff.preAllocatedVUs,
      maxVUs: bff.maxVUs,
    },
  },
  thresholds: {
    http_req_duration: [`p(95)<${bff.p95Ms}`],
    error_rate: [`rate<${bff.maxErrorRate}`],
    http_req_failed: [`rate<${bff.maxHttpFailRate}`],
  },
};

// Test data
const users = [
  { username: "testuser1", password: "PaPassword123" },
  { username: "testuser2", password: "PaPassword456" },
  { username: "testuser3", password: "PaPassword789" },
];

const paymentRequests = [
  { amount: 100.5, currency: "USD", destinationAccount: "ACC123456" },
  { amount: 250.75, currency: "EUR", destinationAccount: "ACC789012" },
  { amount: 500.0, currency: "GBP", destinationAccount: "ACC345678" },
];

export function setup() {
  // Health check before starting the test
  const healthResponse = http.get(`${bff.baseUrl}/health/ready`);
  check(healthResponse, {
    "Health check passed": (r) => r.status === 200,
  });
}

export default function () {
  const user = users[Math.floor(Math.random() * users.length)];
  const paymentRequest =
    paymentRequests[Math.floor(Math.random() * paymentRequests.length)];

  // Test login endpoint (v2)
  testLoginV2(user);

  // Test payment endpoint
  testPayment(paymentRequest);

  // sleep(Math.random() * 2); // Random sleep between 0-2 seconds
}

function testLoginV2(user) {
  const loginPayload = JSON.stringify({
    username: user.username,
    password: user.password,
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
    },
  };

  const response = http.post(
    `${bff.baseUrl}/v2/auth/login`,
    loginPayload,
    params
  );

  let body;
  try {
    body = JSON.parse(response.body);
  } catch {
    body = {};
  }

  const success = check(response, {
    "Login v2 status is 200": (r) => r.status === 200,
    "Login v2 has success field": () => body.isSuccess !== undefined,
    "Login v2 has token.accessToken": () =>
      typeof body.token?.accessToken === "string" &&
      body.token.accessToken.length > 0,
    "Login v2 has token.expiresAt": () =>
      typeof body.token?.expiresAt === "string",
    "Login v2 tokenType is Bearer": () => body.token?.tokenType === "Bearer",
    "Login v2 has user.username": () =>
      typeof body.user?.username === "string" && body.user.username.length > 0,
    "Login v2 response time < 150ms": (r) => r.timings.duration < 150,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

function testPayment(paymentRequest) {
  const paymentPayload = JSON.stringify({
    amount: paymentRequest.amount,
    currency: paymentRequest.currency,
    destinationAccount: paymentRequest.destinationAccount,
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
    },
  };

  const response = http.post(
    `${bff.baseUrl}/v1/payments`,
    paymentPayload,
    params
  );

  let body;
  try {
    body = JSON.parse(response.body);
  } catch {
    body = {};
  }

  const success = check(response, {
    "Payment status is 200": (r) => r.status === 200,
    "Payment has success field": () => body.isSuccess !== undefined,
    "Payment has paymentId": () =>
      typeof body.paymentId === "string" && body.paymentId.length > 0,
    "Payment has providerReference": () =>
      typeof body.providerReference === "string" &&
      body.providerReference.length > 0,
    "Payment has processedAt": () => typeof body.processedAt === "string",
    "Payment response time < 150ms": (r) => r.timings.duration < 150,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

export function teardown(data) {
  // Final health check
  const healthResponse = http.get(`${bff.baseUrl}/health/ready`);
  check(healthResponse, {
    "Final health check passed": (r) => r.status === 200,
  });
}
