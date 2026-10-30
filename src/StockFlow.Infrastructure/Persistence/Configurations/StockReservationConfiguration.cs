using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Orders;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable(
            "stock_reservations",
            table =>
            {
                table.HasCheckConstraint("ck_stock_reservations_quantity", "quantity BETWEEN 1 AND 1000000");
                table.HasCheckConstraint("ck_stock_reservations_status", "status IN ('Active', 'Released')");
                table.HasCheckConstraint(
                    "ck_stock_reservations_released_at",
                    "(status = 'Active' AND released_at_utc IS NULL) OR (status = 'Released' AND released_at_utc IS NOT NULL)");
            });
        builder.HasKey(reservation => new { reservation.OrganizationId, reservation.OrderId, reservation.ProductId })
            .HasName("pk_stock_reservations");

        builder.Property(reservation => reservation.OrganizationId)
            .HasColumnName("organization_id")
            .ValueGeneratedNever();
        builder.Property(reservation => reservation.OrderId)
            .HasColumnName("order_id")
            .ValueGeneratedNever();
        builder.Property(reservation => reservation.WarehouseId)
            .HasColumnName("warehouse_id")
            .ValueGeneratedNever();
        builder.Property(reservation => reservation.ProductId)
            .HasColumnName("product_id")
            .ValueGeneratedNever();
        builder.Property(reservation => reservation.Quantity)
            .HasColumnName("quantity")
            .IsRequired();
        builder.Property(reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(reservation => reservation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(reservation => reservation.ReleasedAtUtc)
            .HasColumnName("released_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<OrderLine>()
            .WithOne()
            .HasForeignKey<StockReservation>(reservation => new
                {
                    reservation.OrganizationId,
                    reservation.OrderId,
                    reservation.ProductId
                })
            .HasPrincipalKey<OrderLine>(line => new { line.OrganizationId, line.OrderId, line.ProductId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(reservation => new
                {
                    reservation.OrganizationId,
                    reservation.OrderId,
                    reservation.WarehouseId,
                    reservation.ExpiresAtUtc
                })
            .HasPrincipalKey(order => new
                {
                    order.OrganizationId,
                    order.Id,
                    order.WarehouseId,
                    order.ReservationExpiresAtUtc
                })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryBalance>()
            .WithMany()
            .HasForeignKey(reservation => new
                {
                    reservation.OrganizationId,
                    reservation.WarehouseId,
                    reservation.ProductId
                })
            .HasPrincipalKey(balance => new
                {
                    balance.OrganizationId,
                    balance.WarehouseId,
                    balance.ProductId
                })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
