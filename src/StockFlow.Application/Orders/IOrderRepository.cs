using StockFlow.Domain.Orders;

namespace StockFlow.Application.Orders;

public interface IOrderRepository
{
    Task<OrderWriteResult> CreateAndReserveAsync(
        CreateOrderPersistenceRequest request,
        CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ReservationExpirationResult> ExpireDueReservationsAsync(
        Guid organizationId,
        DateTimeOffset expiredAtUtc,
        CancellationToken cancellationToken);
}

public sealed record OrderLineRequest(Guid ProductId, long Quantity);

public sealed record CreateOrderPersistenceRequest(
    Guid OrganizationId,
    Guid CreatedByUserId,
    Guid WarehouseId,
    string IdempotencyKey,
    string RequestFingerprint,
    IReadOnlyList<OrderLineRequest> Lines,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ReservationExpiresAtUtc);

public sealed record OrderWriteResult(Order Order, bool WasReplay);

public sealed record ReservationExpirationResult(
    int ExpiredOrders,
    int ReleasedReservations,
    long ReleasedQuantity);
