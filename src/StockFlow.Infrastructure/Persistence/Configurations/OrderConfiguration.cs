using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Orders;
using StockFlow.Domain.Organizations;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "orders",
            table =>
            {
                table.HasCheckConstraint("ck_orders_status", "status IN ('Reserved', 'Expired')");
                table.HasCheckConstraint(
                    "ck_orders_expired_at",
                    "(status = 'Reserved' AND expired_at_utc IS NULL) OR (status = 'Expired' AND expired_at_utc IS NOT NULL)");
                table.HasCheckConstraint("ck_orders_total", "total_amount > 0");
                table.HasCheckConstraint("ck_orders_reservation_window", "reservation_expires_at_utc > created_at_utc");
                table.HasCheckConstraint("ck_orders_currency", "currency ~ '^[A-Z]{3}$'");
                table.HasCheckConstraint("ck_orders_idempotency_key", "idempotency_key ~ '^[A-Za-z0-9._~-]{1,128}$'");
                table.HasCheckConstraint("ck_orders_request_fingerprint", "request_fingerprint ~ '^[0-9A-F]{64}$'");
            });
        builder.HasKey(order => order.Id)
            .HasName("pk_orders");
        builder.HasAlternateKey(order => new { order.OrganizationId, order.Id })
            .HasName("ak_orders_organization_id_id");
        builder.HasAlternateKey(order => new
            {
                order.OrganizationId,
                order.Id,
                order.WarehouseId,
                order.ReservationExpiresAtUtc
            })
            .HasName("ak_orders_organization_order_warehouse_expiration");

        builder.Property(order => order.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(order => order.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(order => order.WarehouseId)
            .HasColumnName("warehouse_id")
            .IsRequired();
        builder.Property(order => order.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();
        builder.Property(order => order.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(Order.MaxIdempotencyKeyLength)
            .IsRequired();
        builder.Property(order => order.RequestFingerprint)
            .HasColumnName("request_fingerprint")
            .HasMaxLength(Order.RequestFingerprintLength)
            .IsRequired();
        builder.Property(order => order.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(order => order.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(order => order.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(28, 2)
            .IsRequired();
        builder.Property(order => order.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(order => order.ReservationExpiresAtUtc)
            .HasColumnName("reservation_expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(order => order.ExpiredAtUtc)
            .HasColumnName("expired_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(order => order.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(order => new { order.OrganizationId, order.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.OrganizationId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrganizationMembership>()
            .WithMany()
            .HasForeignKey(order => new { order.OrganizationId, order.CreatedByUserId })
            .HasPrincipalKey(membership => new { membership.OrganizationId, membership.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => new { line.OrganizationId, line.OrderId })
            .HasPrincipalKey(order => new { order.OrganizationId, order.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => new { order.OrganizationId, order.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_orders_organization_idempotency_key");
        builder.HasIndex(order => new { order.OrganizationId, order.Status, order.ReservationExpiresAtUtc })
            .HasDatabaseName("ix_orders_expiration");
    }
}
