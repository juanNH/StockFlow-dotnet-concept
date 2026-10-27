using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Organizations;

namespace StockFlow.Infrastructure.Persistence;

public sealed class StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockFlowDbContext).Assembly);
    }
}
