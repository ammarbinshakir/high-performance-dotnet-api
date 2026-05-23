namespace HighPerformanceDotNetApi.Application.Products;

public interface IProductCache
{
    Task<IReadOnlyList<ProductSummaryDto>?> GetTopRatedAsync(int count, CancellationToken cancellationToken);
    Task SetTopRatedAsync(int count, IReadOnlyList<ProductSummaryDto> products, TimeSpan ttl, CancellationToken cancellationToken);
}
