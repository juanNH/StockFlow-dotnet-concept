using StockFlow.Domain.Inventory;

namespace StockFlow.Application.Inventory;

public interface IInventoryRepository
{
    Task<StockMovement> ApplyAdjustmentAsync(StockAdjustment adjustment, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(
        Guid organizationId,
        Guid warehouseId,
        CancellationToken cancellationToken);

    Task<StockMovementPage> GetMovementsAsync(
        Guid organizationId,
        Guid warehouseId,
        StockMovementSearchOptions options,
        CancellationToken cancellationToken);
}

public sealed record StockMovementSearchOptions(Guid? ProductId, int Page, int PageSize);

public sealed record StockMovementPage(IReadOnlyList<StockMovement> Items, long TotalCount);
