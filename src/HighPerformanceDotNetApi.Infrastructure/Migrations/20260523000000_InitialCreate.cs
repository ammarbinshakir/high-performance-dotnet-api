using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HighPerformanceDotNetApi.Infrastructure.Migrations;

[Migration("20260523000000_InitialCreate")]
[DbContext(typeof(Data.AppDbContext))]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Sku = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                InventoryCount = table.Column<int>(type: "integer", nullable: false),
                Rating = table.Column<double>(type: "double precision", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_products", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_products_Sku", table: "products", column: "Sku", unique: true);
        migrationBuilder.CreateIndex(name: "ix_products_active_rating_id", table: "products", columns: new[] { "IsActive", "Rating", "Id" });
        migrationBuilder.CreateIndex(name: "ix_products_category_price_id", table: "products", columns: new[] { "Category", "Price", "Id" });
        migrationBuilder.CreateIndex(name: "ix_products_created_at", table: "products", column: "CreatedAt");
        migrationBuilder.Sql("CREATE INDEX ix_products_name_trgm ON products USING gin (\"Name\" gin_trgm_ops);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "products");
    }
}
