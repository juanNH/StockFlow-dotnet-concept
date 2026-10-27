using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehousesAndStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_products_organization_id_id",
                table: "products",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                    table.UniqueConstraint("ak_warehouses_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_warehouses_code", "code ~ '^[A-Z0-9]+(-[A-Z0-9]+)*$'");
                    table.ForeignKey(
                        name: "FK_warehouses_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<long>(type: "bigint", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_balances", x => new { x.organization_id, x.warehouse_id, x.product_id });
                    table.CheckConstraint("ck_inventory_balances_quantity", "quantity_on_hand >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_balances_products_organization_id_product_id",
                        columns: x => new { x.organization_id, x.product_id },
                        principalTable: "products",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_balances_warehouses_organization_id_warehouse_id",
                        columns: x => new { x.organization_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_delta = table.Column<long>(type: "bigint", nullable: false),
                    quantity_after = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.CheckConstraint("ck_stock_movements_delta", "quantity_delta <> 0");
                    table.CheckConstraint("ck_stock_movements_quantity_after", "quantity_after >= 0");
                    table.ForeignKey(
                        name: "FK_stock_movements_AspNetUsers_performed_by_user_id",
                        column: x => x.performed_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_products_organization_id_product_id",
                        columns: x => new { x.organization_id, x.product_id },
                        principalTable: "products",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_warehouses_organization_id_warehouse_id",
                        columns: x => new { x.organization_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_organization_id_product_id",
                table: "inventory_balances",
                columns: new[] { "organization_id", "product_id" });

            migrationBuilder.Sql(
                """
                CREATE FUNCTION prevent_stock_movement_mutation() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'stock_movements is append-only' USING ERRCODE = '55000';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER tr_stock_movements_append_only
                BEFORE UPDATE OR DELETE ON stock_movements
                FOR EACH ROW EXECUTE FUNCTION prevent_stock_movement_mutation();
                """);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_org_warehouse_product_created",
                table: "stock_movements",
                columns: new[] { "organization_id", "warehouse_id", "product_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_organization_id_product_id",
                table: "stock_movements",
                columns: new[] { "organization_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_performed_by_user_id",
                table: "stock_movements",
                column: "performed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_warehouses_organization_code",
                table: "warehouses",
                columns: new[] { "organization_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_stock_movements_append_only ON stock_movements;
                DROP FUNCTION IF EXISTS prevent_stock_movement_mutation();
                """);

            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_products_organization_id_id",
                table: "products");
        }
    }
}
