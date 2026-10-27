using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Organizations;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(
            "warehouses",
            table => table.HasCheckConstraint("ck_warehouses_code", "code ~ '^[A-Z0-9]+(-[A-Z0-9]+)*$'"));
        builder.HasKey(warehouse => warehouse.Id)
            .HasName("pk_warehouses");
        builder.HasAlternateKey(warehouse => new { warehouse.OrganizationId, warehouse.Id })
            .HasName("ak_warehouses_organization_id_id");

        builder.Property(warehouse => warehouse.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(warehouse => warehouse.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(warehouse => warehouse.Code)
            .HasColumnName("code")
            .HasMaxLength(Warehouse.MaxCodeLength)
            .IsRequired();
        builder.Property(warehouse => warehouse.Name)
            .HasColumnName("name")
            .HasMaxLength(Warehouse.MaxNameLength)
            .IsRequired();
        builder.Property(warehouse => warehouse.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(warehouse => warehouse.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(warehouse => new { warehouse.OrganizationId, warehouse.Code })
            .IsUnique()
            .HasDatabaseName("ux_warehouses_organization_code");
    }
}
