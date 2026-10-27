using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Warehouses;

public sealed class GetWarehouseHandler(IWarehouseRepository warehouses, ICurrentTenant currentTenant)
{
    public async Task<WarehouseResponse> HandleAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(
            currentTenant.OrganizationId,
            warehouseId,
            cancellationToken);
        if (warehouse is null)
        {
            throw new WarehouseNotFoundException();
        }

        return WarehouseResponse.From(warehouse);
    }
}
