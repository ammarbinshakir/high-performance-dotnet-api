using System.Net.Http.Json;
using HighPerformanceDotNetApi.Application.Pricing;
using HighPerformanceDotNetApi.Application.Products;
using Microsoft.Extensions.Logging;

namespace HighPerformanceDotNetApi.Infrastructure.Pricing;

public sealed class ResilientPricingClient(HttpClient httpClient, ILogger<ResilientPricingClient> logger) : IProductPricingClient
{
    public async Task<PricingSnapshot> GetPricingAsync(string sku, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<PricingResponse>($"/pricing/{sku}", cancellationToken);
            return response is null
                ? Fallback(sku)
                : new PricingSnapshot(sku, response.Price, "external-pricing-service", false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Pricing dependency failed for {Sku}. Returning fallback pricing.", sku);
            return Fallback(sku);
        }
    }

    private static PricingSnapshot Fallback(string sku)
    {
        return new PricingSnapshot(sku, 0m, "fallback-static-price", true);
    }

    private sealed record PricingResponse(decimal Price);
}
