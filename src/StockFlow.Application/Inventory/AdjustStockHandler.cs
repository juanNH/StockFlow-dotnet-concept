using StockFlow.Application.Abstractions;
using StockFlow.Domain.Inventory;

namespace StockFlow.Application.Inventory;

public sealed class AdjustStockHandler(
    IInventoryRepository inventory,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<StockMovementResponse> HandleAsync(
        AdjustStockCommand command,
        CancellationToken cancellationToken)
    {
        var adjustment = StockAdjustment.Create(
            currentTenant.OrganizationId,
            command.WarehouseId,
            command.ProductId,
            currentTenant.UserId,
            command.QuantityDelta,
            command.Reason,
            timeProvider.GetUtcNow());

        var movement = await inventory.ApplyAdjustmentAsync(adjustment, cancellationToken);
        return StockMovementResponse.From(movement);
    }
}
