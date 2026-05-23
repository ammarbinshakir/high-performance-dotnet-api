using HighPerformanceDotNetApi.Application.Products;

namespace HighPerformanceDotNetApi.Application.Pricing;

public interface IProductPricingClient
{
    Task<PricingSnapshot> GetPricingAsync(string sku, CancellationToken cancellationToken);
}
