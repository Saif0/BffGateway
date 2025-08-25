// Shared configuration for k6 load tests

function env(name, fallback) {
  const v = __ENV[name];
  return v !== undefined ? v : fallback;
}

function num(name, fallback) {
  const v = __ENV[name];
  return v !== undefined ? Number(v) : fallback;
}

export const bff = {
  baseUrl: env("BFF_BASE_URL", "http://localhost:5180"),
  rps: num("BFF_RPS", 1000),
  duration: env("BFF_DURATION", "1m"),
  preAllocatedVUs: num("BFF_PREALLOC_VUS", 200),
  maxVUs: num("BFF_MAX_VUS", 1000),
  p95Ms: num("BFF_P95_MS", 150),
  maxErrorRate: env("BFF_MAX_ERROR_RATE", "0.01"),
  maxHttpFailRate: env("BFF_MAX_HTTP_FAIL_RATE", "0.01"),
};

export const provider = {
  baseUrl: env("PROVIDER_BASE_URL", "http://localhost:5001"),
  authRps: num("AUTH_RPS", 200),
  payRps: num("PAY_RPS", 200),
  duration: env("DURATION", "5m"),
  preAllocatedVUs: num("PREALLOC_VUS", 50),
  maxVUs: num("MAX_VUS", 500),
  authP95Ms: num("PROVIDER_AUTH_P95_MS", 200),
  payP95Ms: num("PROVIDER_PAY_P95_MS", 200),
  maxErrorRate: env("PROVIDER_MAX_ERROR_RATE", "0.01"),
  maxHttpFailRate: env("PROVIDER_MAX_HTTP_FAIL_RATE", "0.01"),
};
