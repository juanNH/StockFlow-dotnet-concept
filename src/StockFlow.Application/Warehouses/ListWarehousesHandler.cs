using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Warehouses;

public sealed class ListWarehousesHandler(IWarehouseRepository warehouses, ICurrentTenant currentTenant)
{
    public async Task<IReadOnlyList<WarehouseResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var items = await warehouses.GetAllAsync(currentTenant.OrganizationId, cancellationToken);
        return items.Select(WarehouseResponse.From).ToArray();
    }
}
