namespace StockFlow.Application.Orders;

public sealed record CreateOrderCommand(
    Guid WarehouseId,
    IReadOnlyList<OrderLineRequest> Lines,
    string IdempotencyKey);
