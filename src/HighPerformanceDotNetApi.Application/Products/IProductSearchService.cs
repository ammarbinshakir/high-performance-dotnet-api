namespace HighPerformanceDotNetApi.Application.Products;

public interface IProductSearchService
{
    Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken);
    Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSummaryDto>> GetCachedTopRatedAsync(int count, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSummaryDto>> GetUncachedTopRatedAsync(int count, CancellationToken cancellationToken);
}
