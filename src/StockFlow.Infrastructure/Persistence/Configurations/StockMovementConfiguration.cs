using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Products;
using StockFlow.Domain.Warehouses;
using StockFlow.Infrastructure.Identity;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable(
            "stock_movements",
            table =>
            {
                table.HasCheckConstraint("ck_stock_movements_delta", "quantity_delta <> 0");
                table.HasCheckConstraint("ck_stock_movements_quantity_after", "quantity_after >= 0");
            });
        builder.HasKey(movement => movement.Id)
            .HasName("pk_stock_movements");

        builder.Property(movement => movement.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(movement => movement.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(movement => movement.WarehouseId)
            .HasColumnName("warehouse_id")
            .IsRequired();
        builder.Property(movement => movement.ProductId)
            .HasColumnName("product_id")
            .IsRequired();
        builder.Property(movement => movement.PerformedByUserId)
            .HasColumnName("performed_by_user_id")
            .IsRequired();
        builder.Property(movement => movement.QuantityDelta)
            .HasColumnName("quantity_delta")
            .IsRequired();
        builder.Property(movement => movement.QuantityAfter)
            .HasColumnName("quantity_after")
            .IsRequired();
        builder.Property(movement => movement.Reason)
            .HasColumnName("reason")
            .HasMaxLength(StockAdjustment.MaxReasonLength)
            .IsRequired();
        builder.Property(movement => movement.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(movement => new { movement.OrganizationId, movement.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.OrganizationId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(movement => new { movement.OrganizationId, movement.ProductId })
            .HasPrincipalKey(product => new { product.OrganizationId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockFlowUser>()
            .WithMany()
            .HasForeignKey(movement => movement.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(movement => new
            {
                movement.OrganizationId,
                movement.WarehouseId,
                movement.ProductId,
                movement.CreatedAtUtc
            })
            .HasDatabaseName("ix_stock_movements_org_warehouse_product_created");
    }
}
