using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectStockMovementTruncate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TRIGGER tr_stock_movements_append_only_truncate
                BEFORE TRUNCATE ON stock_movements
                FOR EACH STATEMENT EXECUTE FUNCTION prevent_stock_movement_mutation();
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS tr_stock_movements_append_only_truncate ON stock_movements;");

        }
    }
}
