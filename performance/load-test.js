import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// Custom metrics
const errorRate = new Rate("error_rate");
const responseTime = new Trend("response_time");

// Test configuration
export const options = {
  scenarios: {
    sustained_load: {
      executor: "constant-arrival-rate",
      rate: 1000, // 1000 iterations per second
      timeUnit: "1s",
      duration: "10m",
      preAllocatedVUs: 200,
      maxVUs: 2000,
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<150"],
    error_rate: ["rate<0.01"],
    http_req_failed: ["rate<0.01"],
  },
};

const BASE_URL = "http://localhost:5000";

// Test data
const users = [
  { username: "testuser1", password: "password123" },
  { username: "testuser2", password: "password456" },
  { username: "testuser3", password: "password789" },
];

const paymentRequests = [
  { amount: 100.5, currency: "USD", destinationAccount: "ACC123456" },
  { amount: 250.75, currency: "EUR", destinationAccount: "ACC789012" },
  { amount: 500.0, currency: "GBP", destinationAccount: "ACC345678" },
];

export function setup() {
  // Health check before starting the test
  const healthResponse = http.get(`${BASE_URL}/health/ready`);
  check(healthResponse, {
    "Health check passed": (r) => r.status === 200,
  });
}

export default function () {
  const user = users[Math.floor(Math.random() * users.length)];
  const paymentRequest =
    paymentRequests[Math.floor(Math.random() * paymentRequests.length)];

  // Test login endpoint (v1)
  testLoginV1(user);

  // Test login endpoint (v2)
  testLoginV2(user);

  // Test payment endpoint
  testPayment(paymentRequest);

  sleep(Math.random() * 2); // Random sleep between 0-2 seconds
}

function testLoginV1(user) {
  const loginPayload = JSON.stringify({
    username: user.username,
    password: user.password,
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
    },
  };

  const response = http.post(`${BASE_URL}/v1/auth/login`, loginPayload, params);

  const success = check(response, {
    "Login v1 status is 200": (r) => r.status === 200,
    "Login v1 has success field": (r) =>
      JSON.parse(r.body).isSuccess !== undefined,
    "Login v1 response time < 150ms": (r) => r.timings.duration < 150,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
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

  const response = http.post(`${BASE_URL}/v2/auth/login`, loginPayload, params);

  const success = check(response, {
    "Login v2 status is 200": (r) => r.status === 200,
    "Login v2 has success field": (r) =>
      JSON.parse(r.body).success !== undefined,
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

  const response = http.post(`${BASE_URL}/v1/payments`, paymentPayload, params);

  const success = check(response, {
    "Payment status is 200": (r) => r.status === 200,
    "Payment has success field": (r) =>
      JSON.parse(r.body).isSuccess !== undefined,
    "Payment response time < 150ms": (r) => r.timings.duration < 150,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

export function teardown(data) {
  // Final health check
  const healthResponse = http.get(`${BASE_URL}/health/ready`);
  check(healthResponse, {
    "Final health check passed": (r) => r.status === 200,
  });
}
