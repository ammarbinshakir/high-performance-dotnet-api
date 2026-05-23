using HighPerformanceDotNetApi.Application.Products;

namespace HighPerformanceDotNetApi.Infrastructure.Caching;

public sealed class NullProductCache : IProductCache
{
    public Task<IReadOnlyList<ProductSummaryDto>?> GetTopRatedAsync(int count, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ProductSummaryDto>?>(null);
    }

    public Task SetTopRatedAsync(int count, IReadOnlyList<ProductSummaryDto> products, TimeSpan ttl, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
