using System.Diagnostics;
using HighPerformanceDotNetApi.Application.Pricing;
using HighPerformanceDotNetApi.Application.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HighPerformanceDotNetApi.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductSearchService productSearchService, IProductPricingClient pricingClient) : ControllerBase
{
    [HttpGet("search/optimized")]
    [EnableRateLimiting("search")]
    [ProducesResponseType(typeof(TimedResponse<CursorPage<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimedResponse<CursorPage<ProductSummaryDto>>>> SearchOptimized(
        [FromQuery] string? term,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductSearchQuery(term, category, minPrice, maxPrice, cursor, pageSize);
        return await MeasureAsync("optimized-keyset-query", () => productSearchService.SearchOptimizedAsync(query, cancellationToken));
    }

    [HttpGet("search/slow")]
    [EnableRateLimiting("search")]
    [ProducesResponseType(typeof(TimedResponse<CursorPage<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimedResponse<CursorPage<ProductSummaryDto>>>> SearchSlow(
        [FromQuery] string? term,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductSearchQuery(term, category, minPrice, maxPrice, cursor, pageSize);
        return await MeasureAsync("slow-in-memory-query", () => productSearchService.SearchSlowAsync(query, cancellationToken));
    }

    [HttpGet("hot/cached")]
    [EnableRateLimiting("burst")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["count"])]
    [ProducesResponseType(typeof(TimedResponse<IReadOnlyList<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimedResponse<IReadOnlyList<ProductSummaryDto>>>> CachedTopRated(
        [FromQuery] int count = 25,
        CancellationToken cancellationToken = default)
    {
        return await MeasureAsync("redis-and-response-cached-top-rated", () => productSearchService.GetCachedTopRatedAsync(count, cancellationToken));
    }

    [HttpGet("hot/non-cached")]
    [EnableRateLimiting("burst")]
    [ProducesResponseType(typeof(TimedResponse<IReadOnlyList<ProductSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimedResponse<IReadOnlyList<ProductSummaryDto>>>> NonCachedTopRated(
        [FromQuery] int count = 25,
        CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        return await MeasureAsync("database-only-top-rated", () => productSearchService.GetUncachedTopRatedAsync(count, cancellationToken));
    }

    [HttpGet("rate-limited")]
    [EnableRateLimiting("burst")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult RateLimited()
    {
        return Ok(new
        {
            Message = "This endpoint is intentionally throttled with a fixed window limiter.",
            Policy = "10 requests per 30 seconds per client IP"
        });
    }

    [HttpGet("pricing/{sku}")]
    [ProducesResponseType(typeof(PricingSnapshot), StatusCodes.Status200OK)]
    public async Task<ActionResult<PricingSnapshot>> Pricing(string sku, CancellationToken cancellationToken)
    {
        return await pricingClient.GetPricingAsync(sku, cancellationToken);
    }

    private static async Task<TimedResponse<T>> MeasureAsync<T>(string strategy, Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action();
        stopwatch.Stop();
        return new TimedResponse<T>(strategy, stopwatch.ElapsedMilliseconds, result);
    }
}

public sealed record TimedResponse<T>(string Strategy, long ElapsedMilliseconds, T Data);
