using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StockFlow.Application.Abstractions;
using StockFlow.Infrastructure.Identity;
using StockFlow.Domain.Organizations;
using StockFlow.Domain.Inventory;
using StockFlow.Domain.Products;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Infrastructure.Persistence;

public sealed class StockFlowDbContext(
    DbContextOptions<StockFlowDbContext> options,
    ICurrentTenant? currentTenant = null)
    : IdentityUserContext<StockFlowUser, Guid>(options)
{
    private Guid CurrentOrganizationId => currentTenant?.OrganizationId ?? Guid.Empty;

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockFlowDbContext).Assembly);
        modelBuilder.Entity<StockFlowUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex");
        modelBuilder.Entity<Product>()
            .HasQueryFilter(product => product.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<Warehouse>()
            .HasQueryFilter(warehouse => warehouse.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<InventoryBalance>()
            .HasQueryFilter(balance => balance.OrganizationId == CurrentOrganizationId);
        modelBuilder.Entity<StockMovement>()
            .HasQueryFilter(movement => movement.OrganizationId == CurrentOrganizationId);
    }
}
