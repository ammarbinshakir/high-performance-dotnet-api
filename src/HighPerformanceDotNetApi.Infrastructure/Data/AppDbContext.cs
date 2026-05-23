using HighPerformanceDotNetApi.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace HighPerformanceDotNetApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Sku).HasMaxLength(32).IsRequired();
            entity.Property(product => product.Name).HasMaxLength(180).IsRequired();
            entity.Property(product => product.Category).HasMaxLength(80).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);

            entity.HasIndex(product => product.Sku).IsUnique();
            entity.HasIndex(product => new { product.Category, product.Price, product.Id })
                .HasDatabaseName("ix_products_category_price_id");
            entity.HasIndex(product => new { product.IsActive, product.Rating, product.Id })
                .HasDatabaseName("ix_products_active_rating_id");
            entity.HasIndex(product => product.CreatedAt)
                .HasDatabaseName("ix_products_created_at");
        });
    }
}
