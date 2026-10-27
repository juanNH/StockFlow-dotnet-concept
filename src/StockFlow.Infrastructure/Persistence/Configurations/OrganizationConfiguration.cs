using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Organizations;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(organization => organization.Id)
            .HasName("pk_organizations");

        builder.Property(organization => organization.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasMaxLength(Organization.MaxNameLength)
            .IsRequired();

        builder.Property(organization => organization.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Organization.MaxSlugLength)
            .IsRequired();

        builder.Property(organization => organization.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasIndex(organization => organization.Slug)
            .IsUnique()
            .HasDatabaseName("ux_organizations_slug");
    }
}
