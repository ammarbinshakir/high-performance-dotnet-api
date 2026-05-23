import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    optimized: {
      executor: "constant-vus",
      vus: 10,
      duration: "45s",
      exec: "optimizedSearch"
    },
    slow: {
      executor: "constant-vus",
      vus: 2,
      duration: "45s",
      startTime: "50s",
      exec: "slowSearch"
    }
  },
  thresholds: {
    "http_req_failed": ["rate<0.01"],
    "http_req_duration{endpoint:optimized}": ["p(90)<250", "p(95)<1000"],
    "http_req_duration{endpoint:slow}": ["p(95)<5000"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";
// Run with RATE_LIMITING_ENABLED=false in docker compose; otherwise this script benchmarks throttling.

export function setup() {
  http.get(`${baseUrl}/api/products/search/optimized?term=product&category=Laptops&pageSize=50`);
  http.get(`${baseUrl}/api/products/search/slow?term=product&category=Laptops&pageSize=50`);
}

export function optimizedSearch() {
  const response = http.get(`${baseUrl}/api/products/search/optimized?term=product&category=Laptops&pageSize=50`, {
    tags: { endpoint: "optimized" }
  });
  check(response, { "optimized status is 200": r => r.status === 200 });
  sleep(1);
}

export function slowSearch() {
  const response = http.get(`${baseUrl}/api/products/search/slow?term=product&category=Laptops&pageSize=50`, {
    tags: { endpoint: "slow" }
  });
  check(response, { "slow status is 200": r => r.status === 200 });
  sleep(1);
}
