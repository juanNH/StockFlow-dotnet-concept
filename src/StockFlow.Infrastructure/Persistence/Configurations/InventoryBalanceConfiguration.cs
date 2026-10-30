using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Products;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable(
            "inventory_balances",
            table =>
            {
                table.HasCheckConstraint("ck_inventory_balances_quantity", "quantity_on_hand >= 0");
                table.HasCheckConstraint(
                    "ck_inventory_balances_reserved",
                    "quantity_reserved >= 0 AND quantity_reserved <= quantity_on_hand");
            });
        builder.HasKey(balance => new { balance.OrganizationId, balance.WarehouseId, balance.ProductId })
            .HasName("pk_inventory_balances");

        builder.Property(balance => balance.OrganizationId)
            .HasColumnName("organization_id")
            .ValueGeneratedNever();
        builder.Property(balance => balance.WarehouseId)
            .HasColumnName("warehouse_id")
            .ValueGeneratedNever();
        builder.Property(balance => balance.ProductId)
            .HasColumnName("product_id")
            .ValueGeneratedNever();
        builder.Property(balance => balance.QuantityOnHand)
            .HasColumnName("quantity_on_hand")
            .IsRequired();
        builder.Property(balance => balance.QuantityReserved)
            .HasColumnName("quantity_reserved")
            .HasDefaultValue(0L)
            .IsRequired();
        builder.Ignore(balance => balance.QuantityAvailable);
        builder.Property(balance => balance.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(balance => new { balance.OrganizationId, balance.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.OrganizationId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(balance => new { balance.OrganizationId, balance.ProductId })
            .HasPrincipalKey(product => new { product.OrganizationId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
