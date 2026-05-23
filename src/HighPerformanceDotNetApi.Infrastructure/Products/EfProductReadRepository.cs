using HighPerformanceDotNetApi.Application.Products;
using HighPerformanceDotNetApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HighPerformanceDotNetApi.Infrastructure.Products;

public sealed class EfProductReadRepository(AppDbContext dbContext) : IProductReadRepository
{
    public async Task<CursorPage<ProductSummaryDto>> SearchOptimizedAsync(ProductSearchQuery query, CancellationToken cancellationToken)
    {
        var cursorId = CursorCodec.Decode(query.Cursor) ?? 0;
        var products = dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.Id > cursorId);

        if (query.Category is not null)
        {
            products = products.Where(product => product.Category == query.Category);
        }

        if (query.MinPrice is not null)
        {
            products = products.Where(product => product.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            products = products.Where(product => product.Price <= query.MaxPrice.Value);
        }

        if (query.Term is not null)
        {
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, $"%{query.Term}%") ||
                EF.Functions.ILike(product.Sku, $"%{query.Term}%"));
        }

        var rows = await products
            .OrderBy(product => product.Id)
            .Take(query.PageSize + 1)
            .Select(product => new ProductSummaryDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Category,
                product.Price,
                product.InventoryCount,
                product.Rating))
            .ToListAsync(cancellationToken);

        return BuildPage(rows, query.PageSize);
    }

    public async Task<CursorPage<ProductSummaryDto>> SearchSlowAsync(ProductSearchQuery query, CancellationToken cancellationToken)
    {
        var cursorId = CursorCodec.Decode(query.Cursor) ?? 0;
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        var filtered = products.AsEnumerable();

        if (query.Category is not null)
        {
            filtered = filtered.Where(product => product.Category == query.Category);
        }

        if (query.MinPrice is not null)
        {
            filtered = filtered.Where(product => product.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            filtered = filtered.Where(product => product.Price <= query.MaxPrice.Value);
        }

        if (query.Term is not null)
        {
            filtered = filtered.Where(product =>
                product.Name.Contains(query.Term, StringComparison.OrdinalIgnoreCase) ||
                product.Sku.Contains(query.Term, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered
            .Where(product => product.Id > cursorId)
            .OrderBy(product => product.Id)
            .Take(query.PageSize + 1)
            .Select(product => new ProductSummaryDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Category,
                product.Price,
                product.InventoryCount,
                product.Rating))
            .ToList();

        return BuildPage(rows, query.PageSize);
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> GetTopRatedAsync(int count, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.Rating)
            .ThenBy(product => product.Id)
            .Take(count)
            .Select(product => new ProductSummaryDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Category,
                product.Price,
                product.InventoryCount,
                product.Rating))
            .ToListAsync(cancellationToken);
    }

    private static CursorPage<ProductSummaryDto> BuildPage(List<ProductSummaryDto> rows, int pageSize)
    {
        var hasNext = rows.Count > pageSize;
        var items = rows.Take(pageSize).ToList();
        var nextCursor = hasNext && items.Count > 0 ? CursorCodec.Encode(items[^1].Id) : null;
        return new CursorPage<ProductSummaryDto>(items, nextCursor, pageSize);
    }
}
