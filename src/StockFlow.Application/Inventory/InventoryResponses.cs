using StockFlow.Domain.Inventory;

namespace StockFlow.Application.Inventory;

public sealed record InventoryBalanceResponse(
    Guid WarehouseId,
    Guid ProductId,
    long QuantityOnHand,
    long QuantityReserved,
    long QuantityAvailable,
    DateTimeOffset UpdatedAtUtc)
{
    public static InventoryBalanceResponse From(InventoryBalance balance)
    {
        return new InventoryBalanceResponse(
            balance.WarehouseId,
            balance.ProductId,
            balance.QuantityOnHand,
            balance.QuantityReserved,
            balance.QuantityAvailable,
            balance.UpdatedAtUtc);
    }
}

public sealed record StockMovementResponse(
    Guid Id,
    Guid WarehouseId,
    Guid ProductId,
    long QuantityDelta,
    long QuantityAfter,
    string Reason,
    Guid PerformedByUserId,
    DateTimeOffset CreatedAtUtc)
{
    public static StockMovementResponse From(StockMovement movement)
    {
        return new StockMovementResponse(
            movement.Id,
            movement.WarehouseId,
            movement.ProductId,
            movement.QuantityDelta,
            movement.QuantityAfter,
            movement.Reason,
            movement.PerformedByUserId,
            movement.CreatedAtUtc);
    }
}

public sealed record StockMovementSearchResult(
    IReadOnlyList<StockMovementResponse> Items,
    int Page,
    int PageSize,
    long TotalCount,
    long TotalPages);
