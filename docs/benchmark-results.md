# Benchmark Results

This file is intentionally set up as a portfolio artifact. Run the load tests after the database has seeded 100k products, then paste screenshots and before/after numbers here.

## Test Environment

- Machine: TODO
- .NET SDK/runtime: TODO
- PostgreSQL version: TODO
- Redis version: TODO
- Dataset size: 100,000 products
- API mode: Release Docker container

## Query Comparison

Command:

```bash
RATE_LIMITING_ENABLED=false docker compose up -d --build
k6 run load-tests/k6-search-comparison.js
```

The optimized and slow scenarios run in separate phases so the slow query does not contaminate optimized-query latency.

| Endpoint | p50 | p95 | p99 | RPS | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| `/api/products/search/slow` | TODO | TODO | TODO | TODO | In-memory filtering after loading active rows |
| `/api/products/search/optimized` | TODO | TODO | TODO | TODO | Keyset pagination, projection, no tracking, indexed predicates |

TODO: Add k6 screenshot.

## Cache Comparison

Command:

```bash
RATE_LIMITING_ENABLED=false docker compose up -d --build
k6 run load-tests/k6-cache-comparison.js
```

| Endpoint | p50 | p95 | p99 | RPS | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| `/api/products/hot/non-cached` | TODO | TODO | TODO | TODO | Database hit on every request |
| `/api/products/hot/cached` | TODO | TODO | TODO | TODO | Redis cache plus ASP.NET response caching |

TODO: Add Redis hit-rate screenshot.

## PostgreSQL Plans

Capture query plans with:

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT "Id", "Sku", "Name", "Category", "Price", "InventoryCount", "Rating"
FROM products
WHERE "IsActive" = true
  AND "Category" = 'Laptops'
  AND "Price" >= 100
  AND "Id" > 0
ORDER BY "Id"
LIMIT 51;
```

TODO: Add slow and optimized query plans.
