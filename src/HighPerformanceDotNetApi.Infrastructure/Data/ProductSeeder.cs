using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HighPerformanceDotNetApi.Infrastructure.Data;

public sealed class ProductSeeder(AppDbContext dbContext, ILogger<ProductSeeder> logger)
{
    private const int TargetRows = 100_000;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Products.CountAsync(cancellationToken);
        if (existing >= TargetRows)
        {
            logger.LogInformation("Seed skipped. Product table already has {ProductCount} rows.", existing);
            return;
        }

        logger.LogInformation("Seeding {TargetRows} products for performance demos.", TargetRows);
        var start = existing + 1;

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO products ("Sku", "Name", "Category", "Price", "InventoryCount", "Rating", "IsActive", "CreatedAt", "UpdatedAt")
            SELECT
                'SKU-' || lpad(i::text, 6, '0') AS "Sku",
                category || ' Performance Product ' || lpad(i::text, 6, '0') AS "Name",
                category AS "Category",
                round((((i * 37) % 490000)::numeric / 100) + 25, 2) AS "Price",
                ((i * 53) % 2500)::integer AS "InventoryCount",
                round(3 + (((i * 17) % 200)::numeric / 100), 2)::double precision AS "Rating",
                i % 17 <> 0 AS "IsActive",
                now() - (i || ' minutes')::interval AS "CreatedAt",
                now() - (((i * 29) % 60000) || ' minutes')::interval AS "UpdatedAt"
            FROM generate_series({start}, {TargetRows}) AS s(i)
            CROSS JOIN LATERAL (
                SELECT (ARRAY['Laptops','Keyboards','Monitors','Storage','Networking','Audio','Components','Accessories'])[(i % 8) + 1] AS category
            ) c
            ON CONFLICT ("Sku") DO NOTHING;
            """, cancellationToken);

        logger.LogInformation("Seeded product table to {TargetRows} rows.", TargetRows);
    }
}
