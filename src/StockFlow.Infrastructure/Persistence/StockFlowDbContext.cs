using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StockFlow.Infrastructure.Identity;
using StockFlow.Domain.Organizations;

namespace StockFlow.Infrastructure.Persistence;

public sealed class StockFlowDbContext(DbContextOptions<StockFlowDbContext> options)
    : IdentityUserContext<StockFlowUser, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockFlowDbContext).Assembly);
        modelBuilder.Entity<StockFlowUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex");
    }
}
