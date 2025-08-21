import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// Environment-driven config with sensible defaults
const BASE_URL = __ENV.PROVIDER_BASE_URL || "http://localhost:5001";
const AUTH_RPS = Number(__ENV.AUTH_RPS || 200);
const PAY_RPS = Number(__ENV.PAY_RPS || 200);
const TEST_DURATION = __ENV.DURATION || "5m";
const PREALLOC_VUS = Number(__ENV.PREALLOC_VUS || 50);
const MAX_VUS = Number(__ENV.MAX_VUS || 500);

// Custom metrics
const authDuration = new Trend("auth_duration");
const payDuration = new Trend("pay_duration");
const authErrorRate = new Rate("auth_error_rate");
const payErrorRate = new Rate("pay_error_rate");

export const options = {
  scenarios: {
    auth_rate: {
      executor: "constant-arrival-rate",
      rate: AUTH_RPS,
      timeUnit: "1s",
      duration: TEST_DURATION,
      preAllocatedVUs: PREALLOC_VUS,
      maxVUs: MAX_VUS,
      exec: "authScenario",
    },
    pay_rate: {
      executor: "constant-arrival-rate",
      rate: PAY_RPS,
      timeUnit: "1s",
      duration: TEST_DURATION,
      preAllocatedVUs: PREALLOC_VUS,
      maxVUs: MAX_VUS,
      exec: "payScenario",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    auth_duration: ["p(95)<200"],
    pay_duration: ["p(95)<200"],
    auth_error_rate: ["rate<0.01"],
    pay_error_rate: ["rate<0.01"],
  },
};

// Simple health probe before and after
export function setup() {
  const res = http.get(`${BASE_URL}/api/authenticate`);
  check(res, { "provider reachable (setup)": (r) => r.status !== 503 });
}

export function teardown() {
  const res = http.get(`${BASE_URL}/api/authenticate`);
  check(res, { "provider reachable (teardown)": (r) => r.status !== 503 });
}

// Auth scenario: POST /api/authenticate
export function authScenario() {
  const payload = JSON.stringify({ user: pickUser(), pwd: "password" });
  const params = { headers: { "Content-Type": "application/json" } };

  const res = http.post(`${BASE_URL}/api/authenticate`, payload, params);

  const ok = check(res, {
    "auth status 200": (r) => r.status === 200,
    "auth has token": (r) => safeJson(r)?.token,
    "auth has expiresAt": (r) => Boolean(safeJson(r)?.expiresAt),
  });

  authErrorRate.add(!ok);
  authDuration.add(res.timings.duration);

  sleep(Math.random() * 0.2);
}

// Payment scenario: POST /api/pay
export function payScenario() {
  const req = pickPayment();
  const payload = JSON.stringify({
    total: req.total,
    curr: req.curr,
    dest: req.dest,
  });
  const params = { headers: { "Content-Type": "application/json" } };

  const res = http.post(`${BASE_URL}/api/pay`, payload, params);

  const ok = check(res, {
    "pay status 200": (r) => r.status === 200,
    "pay has transactionId": (r) => safeJson(r)?.transactionId,
    "pay has providerRef": (r) => safeJson(r)?.providerRef,
  });

  payErrorRate.add(!ok);
  payDuration.add(res.timings.duration);

  sleep(Math.random() * 0.2);
}

function pickUser() {
  const users = ["alice", "bob", "charlie", "diana"];
  return users[Math.floor(Math.random() * users.length)];
}

function pickPayment() {
  const samples = [
    { total: 10.5, curr: "USD", dest: "ACC001" },
    { total: 25.0, curr: "EUR", dest: "ACC002" },
    { total: 50.3, curr: "GBP", dest: "ACC003" },
    { total: 5.0, curr: "USD", dest: "ACC004" },
  ];
  return samples[Math.floor(Math.random() * samples.length)];
}

function safeJson(res) {
  try {
    return JSON.parse(res.body);
  } catch {
    return {};
  }
}
