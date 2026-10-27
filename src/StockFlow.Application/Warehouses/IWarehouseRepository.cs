using StockFlow.Domain.Warehouses;

namespace StockFlow.Application.Warehouses;

public interface IWarehouseRepository
{
    void Add(Warehouse warehouse);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<Warehouse?> GetByIdAsync(Guid organizationId, Guid warehouseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Warehouse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken);
}
