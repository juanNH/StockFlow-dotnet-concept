using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Organizations;
using StockFlow.Infrastructure.Identity;

namespace StockFlow.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable(
            "organization_memberships",
            table => table.HasCheckConstraint(
                "ck_organization_memberships_role",
                "role IN ('Owner', 'Admin', 'Member')"));
        builder.HasKey(membership => membership.Id);
        builder.HasAlternateKey(membership => new { membership.OrganizationId, membership.UserId })
            .HasName("ak_organization_memberships_organization_user");
        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Property(membership => membership.UserId).HasColumnName("user_id");
        builder.Property(membership => membership.OrganizationId).HasColumnName("organization_id");
        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(membership => membership.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StockFlowUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(membership => new { membership.UserId, membership.OrganizationId })
            .IsUnique()
            .HasDatabaseName("ux_organization_memberships_user_organization");
    }
}
