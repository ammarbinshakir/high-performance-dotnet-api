using HighPerformanceDotNetApi.Api.Controllers;
using HighPerformanceDotNetApi.Application.Pricing;
using HighPerformanceDotNetApi.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace HighPerformanceDotNetApi.Api.Tests;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task SearchOptimized_ReturnsTimedCursorPage()
    {
        var controller = new ProductsController(new FakeSearchService(), new FakePricingClient());

        var response = await controller.SearchOptimized("laptop", null, null, null, null, 10, CancellationToken.None);

        var result = Assert.IsType<TimedResponse<CursorPage<ProductSummaryDto>>>(response.Value);
        Assert.Equal("optimized-keyset-query", result.Strategy);
        Assert.Single(result.Data.Items);
        Assert.Equal(10, result.Data.PageSize);
    }

    [Fact]
    public void RateLimited_ReturnsPolicyDescription()
    {
        var controller = new ProductsController(new FakeSearchService(), new FakePricingClient());

        var response = controller.RateLimited();

        Assert.IsType<OkObjectResult>(response);
    }

    private sealed class FakeSearchService : IProductSearchService
    {
        public Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(Page(query.PageSize));
        }

        public Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(Page(query.PageSize));
        }

        public Task<IReadOnlyList<ProductSummaryDto>> GetCachedTopRatedAsync(int count, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProductSummaryDto>>(Page(count).Items);
        }

        public Task<IReadOnlyList<ProductSummaryDto>> GetUncachedTopRatedAsync(int count, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ProductSummaryDto>>(Page(count).Items);
        }

        private static CursorPage<ProductSummaryDto> Page(int pageSize)
        {
            var product = new ProductSummaryDto(1, "SKU-000001", "Product 1", "Laptops", 100, 10, 4.8);
            return new CursorPage<ProductSummaryDto>(new[] { product }, null, pageSize);
        }
    }

    private sealed class FakePricingClient : IProductPricingClient
    {
        public Task<PricingSnapshot> GetPricingAsync(string sku, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PricingSnapshot(sku, 100, "test", false));
        }
    }
}
