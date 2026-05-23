namespace HighPerformanceDotNetApi.Application.Products;

public interface IProductReadRepository
{
    Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken);
    Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSummaryDto>> GetTopRatedAsync(int count, CancellationToken cancellationToken);
}
