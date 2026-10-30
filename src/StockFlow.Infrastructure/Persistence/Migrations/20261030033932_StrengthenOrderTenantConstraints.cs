using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenOrderTenantConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_AspNetUsers_created_by_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_organization_memberships_organization_id",
                table: "organization_memberships");

            migrationBuilder.DropIndex(
                name: "IX_orders_created_by_user_id",
                table: "orders");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_organization_memberships_organization_user",
                table: "organization_memberships",
                columns: new[] { "organization_id", "user_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_orders_organization_order_warehouse_expiration",
                table: "orders",
                columns: new[] { "organization_id", "id", "warehouse_id", "reservation_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_organization_id_order_id_warehouse_id_ex~",
                table: "stock_reservations",
                columns: new[] { "organization_id", "order_id", "warehouse_id", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_organization_id_created_by_user_id",
                table: "orders",
                columns: new[] { "organization_id", "created_by_user_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_orders_organization_memberships_organization_id_created_by_~",
                table: "orders",
                columns: new[] { "organization_id", "created_by_user_id" },
                principalTable: "organization_memberships",
                principalColumns: new[] { "organization_id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_reservations_orders_organization_id_order_id_warehous~",
                table: "stock_reservations",
                columns: new[] { "organization_id", "order_id", "warehouse_id", "expires_at_utc" },
                principalTable: "orders",
                principalColumns: new[] { "organization_id", "id", "warehouse_id", "reservation_expires_at_utc" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_organization_memberships_organization_id_created_by_~",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_reservations_orders_organization_id_order_id_warehous~",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_organization_id_order_id_warehouse_id_ex~",
                table: "stock_reservations");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_organization_memberships_organization_user",
                table: "organization_memberships");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_orders_organization_order_warehouse_expiration",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_organization_id_created_by_user_id",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_organization_memberships_organization_id",
                table: "organization_memberships",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by_user_id",
                table: "orders",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_AspNetUsers_created_by_user_id",
                table: "orders",
                column: "created_by_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
