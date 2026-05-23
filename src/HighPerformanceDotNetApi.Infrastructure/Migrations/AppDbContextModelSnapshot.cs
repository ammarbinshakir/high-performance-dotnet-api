using System;
using HighPerformanceDotNetApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HighPerformanceDotNetApi.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("HighPerformanceDotNetApi.Domain.Products.Product", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint");

            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

            b.Property<string>("Category").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<int>("InventoryCount").HasColumnType("integer");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(180).HasColumnType("character varying(180)");
            b.Property<decimal>("Price").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<double>("Rating").HasColumnType("double precision");
            b.Property<string>("Sku").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");

            b.HasKey("Id");
            b.HasIndex("Sku").IsUnique();
            b.HasIndex("IsActive", "Rating", "Id").HasDatabaseName("ix_products_active_rating_id");
            b.HasIndex("Category", "Price", "Id").HasDatabaseName("ix_products_category_price_id");
            b.HasIndex("CreatedAt").HasDatabaseName("ix_products_created_at");
            b.ToTable("products");
        });
    }
}
