using HighPerformanceDotNetApi.Application.Products;

namespace HighPerformanceDotNetApi.Application.Tests;

public sealed class ProductSearchServiceTests
{
    [Fact]
    public async Task GetCachedTopRated_ReturnsCachedProductsWithoutRepositoryHit()
    {
        var cached = new[] { Product(1) };
        var repository = new FakeRepository();
        var cache = new FakeCache(cached);
        var service = new ProductSearchService(repository, cache);

        var result = await service.GetCachedTopRatedAsync(25, CancellationToken.None);

        Assert.Same(cached, result);
        Assert.Equal(0, repository.TopRatedCalls);
    }

    [Fact]
    public async Task SearchOptimized_NormalizesPageSizeAndWhitespace()
    {
        var repository = new FakeRepository();
        var service = new ProductSearchService(repository, new FakeCache(null));

        await service.SearchOptimizedAsync(new ProductSearchQuery("  laptop  ", "  Laptops  ", null, null, null, 999), CancellationToken.None);

        Assert.NotNull(repository.LastOptimizedQuery);
        Assert.Equal("laptop", repository.LastOptimizedQuery.Term);
        Assert.Equal("Laptops", repository.LastOptimizedQuery.Category);
        Assert.Equal(200, repository.LastOptimizedQuery.PageSize);
    }

    private static ProductSummaryDto Product(long id)
    {
        return new ProductSummaryDto(id, $"SKU-{id:000000}", $"Product {id}", "Laptops", 100, 10, 4.8);
    }

    private sealed class FakeRepository : IProductReadRepository
    {
        public int TopRatedCalls { get; private set; }
        public ProductSearchQuery? LastOptimizedQuery { get; private set; }

        public Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken)
        {
            LastOptimizedQuery = query;
            return Task.FromResult(new CursorPage<ProductSummaryDto>(Array.Empty<ProductSummaryDto>(), null, query.PageSize));
        }

        public Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CursorPage<ProductSummaryDto>(Array.Empty<ProductSummaryDto>(), null, query.PageSize));
        }

        public Task<IReadOnlyList<ProductSummaryDto>> GetTopRatedAsync(int count, CancellationToken cancellationToken)
        {
            TopRatedCalls++;
            return Task.FromResult<IReadOnlyList<ProductSummaryDto>>(new[] { Product(2) });
        }
    }

    private sealed class FakeCache(IReadOnlyList<ProductSummaryDto>? cached) : IProductCache
    {
        public Task<IReadOnlyList<ProductSummaryDto>?> GetTopRatedAsync(int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(cached);
        }

        public Task SetTopRatedAsync(int count, IReadOnlyList<ProductSummaryDto> products, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
