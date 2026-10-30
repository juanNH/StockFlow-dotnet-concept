using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Orders;
using StockFlow.Domain.Products;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable(
            "order_lines",
            table =>
            {
                table.HasCheckConstraint("ck_order_lines_quantity", "quantity BETWEEN 1 AND 1000000");
                table.HasCheckConstraint("ck_order_lines_unit_price", "unit_price > 0");
                table.HasCheckConstraint("ck_order_lines_total", "line_total > 0");
                table.HasCheckConstraint("ck_order_lines_total_matches_snapshot", "line_total = unit_price * quantity");
                table.HasCheckConstraint("ck_order_lines_unit_price_scale", "unit_price = trunc(unit_price, 2)");
                table.HasCheckConstraint("ck_order_lines_currency", "currency ~ '^[A-Z]{3}$'");
            });
        builder.HasKey(line => new { line.OrganizationId, line.OrderId, line.ProductId })
            .HasName("pk_order_lines");

        builder.Property(line => line.OrganizationId)
            .HasColumnName("organization_id")
            .ValueGeneratedNever();
        builder.Property(line => line.OrderId)
            .HasColumnName("order_id")
            .ValueGeneratedNever();
        builder.Property(line => line.ProductId)
            .HasColumnName("product_id")
            .ValueGeneratedNever();
        builder.Property(line => line.ProductSku)
            .HasColumnName("product_sku")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(line => line.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(line => line.Quantity)
            .HasColumnName("quantity")
            .IsRequired();
        builder.Property(line => line.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(20, 4)
            .IsRequired();
        builder.Property(line => line.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(line => line.LineTotal)
            .HasColumnName("line_total")
            .HasPrecision(28, 2)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(line => new { line.OrganizationId, line.ProductId })
            .HasPrincipalKey(product => new { product.OrganizationId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
