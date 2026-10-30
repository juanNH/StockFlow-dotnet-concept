using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "quantity_reserved",
                table: "inventory_balances",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(28,2)", precision: 28, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reservation_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.UniqueConstraint("ak_orders_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_orders_currency", "currency ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_orders_expired_at", "(status = 'Reserved' AND expired_at_utc IS NULL) OR (status = 'Expired' AND expired_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_orders_idempotency_key", "idempotency_key ~ '^[A-Za-z0-9._~-]{1,128}$'");
                    table.CheckConstraint("ck_orders_request_fingerprint", "request_fingerprint ~ '^[0-9A-F]{64}$'");
                    table.CheckConstraint("ck_orders_reservation_window", "reservation_expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_orders_status", "status IN ('Reserved', 'Expired')");
                    table.CheckConstraint("ck_orders_total", "total_amount > 0");
                    table.ForeignKey(
                        name: "FK_orders_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_warehouses_organization_id_warehouse_id",
                        columns: x => new { x.organization_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    quantity = table.Column<long>(type: "bigint", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(28,2)", precision: 28, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => new { x.organization_id, x.order_id, x.product_id });
                    table.CheckConstraint("ck_order_lines_currency", "currency ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_order_lines_quantity", "quantity BETWEEN 1 AND 1000000");
                    table.CheckConstraint("ck_order_lines_total", "line_total > 0");
                    table.CheckConstraint("ck_order_lines_total_matches_snapshot", "line_total = unit_price * quantity");
                    table.CheckConstraint("ck_order_lines_unit_price", "unit_price > 0");
                    table.CheckConstraint("ck_order_lines_unit_price_scale", "unit_price = trunc(unit_price, 2)");
                    table.ForeignKey(
                        name: "FK_order_lines_orders_organization_id_order_id",
                        columns: x => new { x.organization_id, x.order_id },
                        principalTable: "orders",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_lines_products_organization_id_product_id",
                        columns: x => new { x.organization_id, x.product_id },
                        principalTable: "products",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_reservations", x => new { x.organization_id, x.order_id, x.product_id });
                    table.CheckConstraint("ck_stock_reservations_quantity", "quantity BETWEEN 1 AND 1000000");
                    table.CheckConstraint("ck_stock_reservations_released_at", "(status = 'Active' AND released_at_utc IS NULL) OR (status = 'Released' AND released_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_stock_reservations_status", "status IN ('Active', 'Released')");
                    table.ForeignKey(
                        name: "FK_stock_reservations_inventory_balances_organization_id_wareh~",
                        columns: x => new { x.organization_id, x.warehouse_id, x.product_id },
                        principalTable: "inventory_balances",
                        principalColumns: new[] { "organization_id", "warehouse_id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_order_lines_organization_id_order_id_pro~",
                        columns: x => new { x.organization_id, x.order_id, x.product_id },
                        principalTable: "order_lines",
                        principalColumns: new[] { "organization_id", "order_id", "product_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_balances_reserved",
                table: "inventory_balances",
                sql: "quantity_reserved >= 0 AND quantity_reserved <= quantity_on_hand");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_organization_id_product_id",
                table: "order_lines",
                columns: new[] { "organization_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by_user_id",
                table: "orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_expiration",
                table: "orders",
                columns: new[] { "organization_id", "status", "reservation_expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_organization_id_warehouse_id",
                table: "orders",
                columns: new[] { "organization_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ux_orders_organization_idempotency_key",
                table: "orders",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_organization_id_warehouse_id_product_id",
                table: "stock_reservations",
                columns: new[] { "organization_id", "warehouse_id", "product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropTable(
                name: "order_lines");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_balances_reserved",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "quantity_reserved",
                table: "inventory_balances");
        }
    }
}
