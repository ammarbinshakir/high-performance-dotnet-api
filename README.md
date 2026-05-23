# High Performance .NET API

Production-style ASP.NET Core 8 Web API built to showcase backend performance engineering concepts, not generic CRUD.

## What This Demonstrates

- Large product/search dataset with 100k seeded rows
- Slow vs optimized EF Core query paths
- PostgreSQL indexing examples, including composite and trigram indexes
- Cursor-based keyset pagination
- Redis application caching
- ASP.NET Core response caching
- Rate limiting and throttling
- Polly retry plus circuit breaker fallback for a dependency call
- Structured logs with Serilog
- Docker Compose for API, PostgreSQL, and Redis
- xUnit tests for application services and controllers
- k6 load-testing scripts and benchmark documentation placeholders

## Architecture

```text
src/
  HighPerformanceDotNetApi.Api             HTTP API, Swagger, middleware, controllers
  HighPerformanceDotNetApi.Application     Use cases, DTOs, contracts
  HighPerformanceDotNetApi.Infrastructure  EF Core, PostgreSQL, Redis, Polly clients
  HighPerformanceDotNetApi.Domain          Product domain model
tests/
  HighPerformanceDotNetApi.Application.Tests
  HighPerformanceDotNetApi.Api.Tests
```

## Quick Start

### Option 1: Run Everything With Docker

This starts the API, PostgreSQL, and Redis:

```bash
docker compose up --build
```

The API starts at:

- Swagger: `http://localhost:8080/swagger`
- Optimized search: `http://localhost:8080/api/products/search/optimized?term=product&category=Laptops&pageSize=50`
- Slow search: `http://localhost:8080/api/products/search/slow?term=product&category=Laptops&pageSize=50`
- Cached hot products: `http://localhost:8080/api/products/hot/cached?count=25`
- Non-cached hot products: `http://localhost:8080/api/products/hot/non-cached?count=25`

On startup the API applies migrations and seeds 100,000 products. The first boot can take a little while because the seed is intentionally large.

Stop the stack with:

```bash
docker compose down
```

Reset the database and seed from scratch with:

```bash
docker compose down -v
docker compose up --build
```

### Option 2: Run PostgreSQL/Redis In Docker And API Locally

Start only the dependencies:

```bash
docker compose up -d postgres redis
```

Run the API from the repo root:

```bash
dotnet run --project src/HighPerformanceDotNetApi.Api --launch-profile http
```

The local API starts at:

- Swagger: `http://localhost:5080/swagger`
- Optimized search: `http://localhost:5080/api/products/search/optimized?term=product&category=Laptops&pageSize=50`
- Slow search: `http://localhost:5080/api/products/search/slow?term=product&category=Laptops&pageSize=50`
- Cached hot products: `http://localhost:5080/api/products/hot/cached?count=25`

The local API uses these default connection strings from `appsettings.json`:

- PostgreSQL: `localhost:55432`
- Redis: `localhost:6379`

### First Run Notes

- The API automatically applies EF Core migrations.
- The product seeder inserts 100,000 rows the first time it sees an empty database.
- If Swagger opens before seeding finishes, wait for the API logs to show the seed progress completing.
- Do not run the Docker API and local API on the same port. Docker uses `8080`; local `dotnet run` uses `5080`.

## Performance Concepts

### Slow Query Endpoint

`/api/products/search/slow` intentionally loads active products into memory, then applies filtering and ordering. This demonstrates the common performance failure mode of moving too much work from PostgreSQL into the application process.

### Optimized Query Endpoint

`/api/products/search/optimized` uses:

- `AsNoTracking()` for read-only queries
- Server-side filtering
- Projection into a compact DTO
- Keyset pagination with an encoded cursor
- Composite indexes for category, price, active state, rating, and ID

### Compare Slow vs Optimized

Use the `elapsedMilliseconds` field in the JSON response. Swagger/browser timing includes UI rendering, connection reuse, and response transfer, so it can hide query-level differences.

```bash
curl -s "http://localhost:8080/api/products/search/optimized?pageSize=50"
curl -s "http://localhost:8080/api/products/search/slow?pageSize=50"
```

For a more realistic filtered search:

```bash
curl -s "http://localhost:8080/api/products/search/optimized?term=Performance%20Product&category=Laptops&minPrice=100&pageSize=50"
curl -s "http://localhost:8080/api/products/search/slow?term=Performance%20Product&category=Laptops&minPrice=100&pageSize=50"
```

Typical local Docker result after warm-up:

- Optimized search: single-digit to low double-digit milliseconds
- Slow search: hundreds of milliseconds because it materializes and filters a much larger set in memory

### Caching Endpoints

`/api/products/hot/non-cached` always reads from PostgreSQL.

`/api/products/hot/cached` uses Redis for application-level caching and ASP.NET Core response caching for repeated HTTP responses.

### Rate Limiting

`/api/products/rate-limited` uses a strict fixed-window limiter. Search endpoints use a sliding-window limiter to smooth bursts while allowing normal browsing traffic.

### Polly Resilience

`/api/products/pricing/{sku}` calls a simulated external pricing dependency through `HttpClient` with retry and circuit breaker policies. When the dependency fails, the client returns a fallback result with `isFallback = true`.

## Database Indexes

The initial migration creates:

- Unique SKU index
- `ix_products_category_price_id` for filtered product search
- `ix_products_active_rating_id` for top-rated hot-product queries
- `ix_products_created_at` for time-ordered exploration
- `ix_products_name_trgm` for text search examples using PostgreSQL `pg_trgm`

## Load Tests

For benchmark runs, disable API rate limiting first. Otherwise k6 will measure throttling and queueing instead of query performance.

```bash
RATE_LIMITING_ENABLED=false docker compose up -d --build
```

Then run:

```bash
k6 run load-tests/k6-search-comparison.js
k6 run load-tests/k6-cache-comparison.js
```

The search comparison script runs optimized traffic first and the slow endpoint second. That keeps the intentionally inefficient endpoint from saturating the API while the optimized endpoint is being measured.

Turn rate limiting back on for the feature demo with:

```bash
docker compose up -d --build
```

Benchmark notes live in [docs/benchmark-results.md](docs/benchmark-results.md). TODO sections are included there for real screenshots, query plans, p95 latency, RPS, and Redis hit-rate evidence.

## Tests

```bash
dotnet test
```

## Portfolio TODOs

- Add real k6 screenshots to `docs/benchmark-results.md`
- Add `EXPLAIN (ANALYZE, BUFFERS)` screenshots for slow and optimized query paths
- Add Docker stats or Grafana screenshots during load tests
- Add a short write-up comparing p95 latency before and after cache warm-up
