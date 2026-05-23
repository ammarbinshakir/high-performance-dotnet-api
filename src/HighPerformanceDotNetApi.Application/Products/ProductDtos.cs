namespace HighPerformanceDotNetApi.Application.Products;

public sealed record ProductSummaryDto(
    long Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int InventoryCount,
    double Rating);

public sealed record ProductSearchQuery(
    string? Term,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Cursor,
    int PageSize = 50);

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, int PageSize);

public sealed record PricingSnapshot(string Sku, decimal Price, string Source, bool IsFallback);
