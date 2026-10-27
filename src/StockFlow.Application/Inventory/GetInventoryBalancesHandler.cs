using StockFlow.Application.Abstractions;
using StockFlow.Application.Warehouses;

namespace StockFlow.Application.Inventory;

public sealed class GetInventoryBalancesHandler(
    IInventoryRepository inventory,
    IWarehouseRepository warehouses,
    ICurrentTenant currentTenant)
{
    public async Task<IReadOnlyList<InventoryBalanceResponse>> HandleAsync(
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(
            currentTenant.OrganizationId,
            warehouseId,
            cancellationToken);
        if (warehouse is null)
        {
            throw new WarehouseNotFoundException();
        }

        var balances = await inventory.GetBalancesAsync(
            currentTenant.OrganizationId,
            warehouseId,
            cancellationToken);
        return balances.Select(InventoryBalanceResponse.From).ToArray();
    }
}
