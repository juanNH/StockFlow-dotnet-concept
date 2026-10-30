namespace StockFlow.Domain.Orders;

public sealed class StockReservation
{
    private StockReservation()
    {
    }

    private StockReservation(
        Guid organizationId,
        Guid orderId,
        Guid warehouseId,
        Guid productId,
        long quantity,
        ReservationStatus status,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? releasedAtUtc)
    {
        OrganizationId = organizationId;
        OrderId = orderId;
        WarehouseId = warehouseId;
        ProductId = productId;
        Quantity = quantity;
        Status = status;
        ExpiresAtUtc = expiresAtUtc;
        ReleasedAtUtc = releasedAtUtc;
    }

    public Guid OrganizationId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public long Quantity { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public static StockReservation Create(
        Guid organizationId,
        Guid orderId,
        Guid warehouseId,
        Guid productId,
        long quantity,
        DateTimeOffset expiresAtUtc)
    {
        if (organizationId == Guid.Empty || orderId == Guid.Empty || warehouseId == Guid.Empty || productId == Guid.Empty)
        {
            throw new OrderRuleViolationException("Organization, order, warehouse, and product are required for a reservation.");
        }

        if (quantity is < 1 or > 1_000_000)
        {
            throw new OrderRuleViolationException("Reservation quantity must be between 1 and 1,000,000.");
        }

        return new StockReservation(
            organizationId,
            orderId,
            warehouseId,
            productId,
            quantity,
            ReservationStatus.Active,
            expiresAtUtc.ToUniversalTime(),
            releasedAtUtc: null);
    }

    public static StockReservation Restore(
        Guid organizationId,
        Guid orderId,
        Guid warehouseId,
        Guid productId,
        long quantity,
        ReservationStatus status,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? releasedAtUtc)
    {
        return new StockReservation(
            organizationId,
            orderId,
            warehouseId,
            productId,
            quantity,
            status,
            expiresAtUtc,
            releasedAtUtc);
    }

    public bool Release(DateTimeOffset releasedAtUtc)
    {
        if (Status == ReservationStatus.Released)
        {
            return false;
        }

        if (Status != ReservationStatus.Active)
        {
            throw new OrderInvalidStateException();
        }

        var timestamp = releasedAtUtc.ToUniversalTime();
        if (timestamp < ExpiresAtUtc)
        {
            throw new OrderReservationNotDueException();
        }

        Status = ReservationStatus.Released;
        ReleasedAtUtc = timestamp;
        return true;
    }
}
