import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  vus: 30,
  duration: "60s",
  thresholds: {
    "http_req_failed": ["rate<0.01"],
    "http_req_duration{endpoint:cached}": ["p(95)<120"],
    "http_req_duration{endpoint:noncached}": ["p(95)<500"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export default function () {
  const cached = http.get(`${baseUrl}/api/products/hot/cached?count=25`, {
    tags: { endpoint: "cached" }
  });
  check(cached, { "cached status is 200": r => r.status === 200 });

  const noncached = http.get(`${baseUrl}/api/products/hot/non-cached?count=25`, {
    tags: { endpoint: "noncached" }
  });
  check(noncached, { "non-cached status is 200": r => r.status === 200 });

  sleep(1);
}
