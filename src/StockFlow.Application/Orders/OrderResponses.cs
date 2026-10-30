using StockFlow.Domain.Orders;

namespace StockFlow.Application.Orders;

public sealed record OrderLineResponse(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    long Quantity,
    decimal UnitPrice,
    string Currency,
    decimal LineTotal)
{
    public static OrderLineResponse From(OrderLine line)
    {
        return new OrderLineResponse(
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Quantity,
            line.UnitPrice,
            line.Currency,
            line.LineTotal);
    }
}

public sealed record OrderResponse(
    Guid Id,
    Guid WarehouseId,
    Guid CreatedByUserId,
    string Status,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ReservationExpiresAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    IReadOnlyList<OrderLineResponse> Lines)
{
    public static OrderResponse From(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.WarehouseId,
            order.CreatedByUserId,
            order.Status.ToString(),
            order.Currency,
            order.TotalAmount,
            order.CreatedAtUtc,
            order.ReservationExpiresAtUtc,
            order.ExpiredAtUtc,
            order.Lines.Select(OrderLineResponse.From).ToArray());
    }
}

public sealed record CreateOrderResult(OrderResponse Order, bool WasReplay);

public sealed record ReservationExpirationResponse(
    int ExpiredOrders,
    int ReleasedReservations,
    long ReleasedQuantity)
{
    public static ReservationExpirationResponse From(ReservationExpirationResult result)
    {
        return new ReservationExpirationResponse(
            result.ExpiredOrders,
            result.ReleasedReservations,
            result.ReleasedQuantity);
    }
}
