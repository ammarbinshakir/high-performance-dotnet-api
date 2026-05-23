namespace HighPerformanceDotNetApi.Application.Products;

public sealed class ProductSearchService(IProductReadRepository repository, IProductCache cache) : IProductSearchService
{
    private static readonly TimeSpan HotProductsTtl = TimeSpan.FromMinutes(5);

    public Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken)
    {
        return repository.SearchOptimizedAsync(Normalize(query), cancellationToken);
    }

    public Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken)
    {
        return repository.SearchSlowAsync(Normalize(query), cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> GetCachedTopRatedAsync(int count, CancellationToken cancellationToken)
    {
        count = Math.Clamp(count, 1, 100);
        var cached = await cache.GetTopRatedAsync(count, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var products = await repository.GetTopRatedAsync(count, cancellationToken);
        await cache.SetTopRatedAsync(count, products, HotProductsTtl, cancellationToken);
        return products;
    }

    public Task<IReadOnlyList<ProductSummaryDto>> GetUncachedTopRatedAsync(int count, CancellationToken cancellationToken)
    {
        return repository.GetTopRatedAsync(Math.Clamp(count, 1, 100), cancellationToken);
    }

    private static ProductSearchQuery Normalize(ProductSearchQuery query)
    {
        return query with
        {
            Term = string.IsNullOrWhiteSpace(query.Term) ? null : query.Term.Trim(),
            Category = string.IsNullOrWhiteSpace(query.Category) ? null : query.Category.Trim(),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };
    }
}
