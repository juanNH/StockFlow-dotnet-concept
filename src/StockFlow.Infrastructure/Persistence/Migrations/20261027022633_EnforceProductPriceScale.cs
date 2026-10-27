using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceProductPriceScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_unit_price_scale",
                table: "products",
                sql: "unit_price = trunc(unit_price, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_unit_price_scale",
                table: "products");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,4)",
                oldPrecision: 20,
                oldScale: 4);
        }
    }
}
