using StockFlow.Application.Abstractions;
using StockFlow.Domain.Warehouses;

namespace StockFlow.Application.Warehouses;

public sealed class CreateWarehouseHandler(
    IWarehouseRepository warehouses,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<WarehouseResponse> HandleAsync(
        CreateWarehouseCommand command,
        CancellationToken cancellationToken)
    {
        var warehouse = Warehouse.Create(
            currentTenant.OrganizationId,
            command.Code,
            command.Name,
            timeProvider.GetUtcNow());

        warehouses.Add(warehouse);
        await warehouses.SaveChangesAsync(cancellationToken);
        return WarehouseResponse.From(warehouse);
    }
}
