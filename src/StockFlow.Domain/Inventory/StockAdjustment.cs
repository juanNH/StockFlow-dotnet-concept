namespace StockFlow.Domain.Inventory;

public sealed class StockAdjustment
{
    public const int MaxReasonLength = 250;

    private StockAdjustment(
        Guid id,
        Guid organizationId,
        Guid warehouseId,
        Guid productId,
        Guid performedByUserId,
        long quantityDelta,
        string reason,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        WarehouseId = warehouseId;
        ProductId = productId;
        PerformedByUserId = performedByUserId;
        QuantityDelta = quantityDelta;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganizationId { get; }

    public Guid WarehouseId { get; }

    public Guid ProductId { get; }

    public Guid PerformedByUserId { get; }

    public long QuantityDelta { get; }

    public string Reason { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static StockAdjustment Create(
        Guid organizationId,
        Guid warehouseId,
        Guid productId,
        Guid performedByUserId,
        long quantityDelta,
        string reason,
        DateTimeOffset createdAtUtc)
    {
        if (organizationId == Guid.Empty || warehouseId == Guid.Empty || productId == Guid.Empty || performedByUserId == Guid.Empty)
        {
            throw new StockAdjustmentRuleViolationException("Organization, warehouse, product, and actor are required.");
        }

        if (quantityDelta == 0)
        {
            throw new StockAdjustmentRuleViolationException("Quantity delta cannot be zero.");
        }

        var normalizedReason = NormalizeReason(reason);

        return new StockAdjustment(
            Guid.NewGuid(),
            organizationId,
            warehouseId,
            productId,
            performedByUserId,
            quantityDelta,
            normalizedReason,
            createdAtUtc.ToUniversalTime());
    }

    public StockMovement ToMovement(long quantityAfter)
    {
        if (quantityAfter < 0)
        {
            throw new StockAdjustmentRuleViolationException("Resulting stock quantity cannot be negative.");
        }

        return StockMovement.Create(this, quantityAfter);
    }

    private static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new StockAdjustmentRuleViolationException("Adjustment reason is required.");
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length > MaxReasonLength)
        {
            throw new StockAdjustmentRuleViolationException(
                $"Adjustment reason cannot exceed {MaxReasonLength} characters.");
        }

        return normalizedReason;
    }
}
