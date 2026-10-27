using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Organizations;
using StockFlow.Domain.Products;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "products",
            table =>
            {
                table.HasCheckConstraint("ck_products_sku", "sku ~ '^[A-Z0-9]+(-[A-Z0-9]+)*$'");
                table.HasCheckConstraint("ck_products_currency", "currency ~ '^[A-Z]{3}$'");
                table.HasCheckConstraint("ck_products_unit_price", "unit_price > 0");
                table.HasCheckConstraint("ck_products_unit_price_scale", "unit_price = trunc(unit_price, 2)");
            });
        builder.HasKey(product => product.Id)
            .HasName("pk_products");

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(product => product.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();
        builder.Property(product => product.Sku)
            .HasColumnName("sku")
            .HasMaxLength(Product.MaxSkuLength)
            .IsRequired();
        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();
        builder.Property(product => product.Description)
            .HasColumnName("description")
            .HasMaxLength(Product.MaxDescriptionLength);
        builder.Property(product => product.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(20, 4)
            .IsRequired();
        builder.Property(product => product.Currency)
            .HasColumnName("currency")
            .HasMaxLength(Product.CurrencyLength)
            .IsRequired();
        builder.Property(product => product.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(product => product.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(product => product.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(product => product.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(product => new { product.OrganizationId, product.Sku })
            .IsUnique()
            .HasDatabaseName("ux_products_organization_sku");
        builder.HasIndex(product => new { product.OrganizationId, product.Name })
            .HasDatabaseName("ix_products_organization_name");
    }
}
