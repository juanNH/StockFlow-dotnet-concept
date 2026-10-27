using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Warehouses;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Infrastructure.Persistence;

internal sealed class EfWarehouseRepository(
    StockFlowDbContext dbContext,
    ICurrentTenant currentTenant) : IWarehouseRepository
{
    public void Add(Warehouse warehouse)
    {
        if (warehouse.OrganizationId != currentTenant.OrganizationId)
        {
            throw new InvalidOperationException("A warehouse can only be added to the current organization.");
        }

        dbContext.Warehouses.Add(warehouse);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsWarehouseCodeConflict(exception))
        {
            throw new WarehouseCodeConflictException();
        }
    }

    public Task<Warehouse?> GetByIdAsync(
        Guid organizationId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Warehouses.SingleOrDefaultAsync(
            warehouse => warehouse.OrganizationId == organizationId && warehouse.Id == warehouseId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.OrganizationId == organizationId)
            .OrderBy(warehouse => warehouse.Code)
            .ToArrayAsync(cancellationToken);
    }

    private static bool IsWarehouseCodeConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_warehouses_organization_code"
        };
    }
}
