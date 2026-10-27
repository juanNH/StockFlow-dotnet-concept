using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockFlow.Infrastructure.Persistence;

public sealed class StockFlowDbContextFactory : IDesignTimeDbContextFactory<StockFlowDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=stockflow;Username=stockflow;Password=stockflow_dev";

    public StockFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new StockFlowDbContext(options);
    }
}
