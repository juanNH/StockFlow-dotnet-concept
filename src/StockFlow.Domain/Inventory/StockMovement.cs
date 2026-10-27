namespace StockFlow.Domain.Inventory;

public sealed class StockMovement
{
    private StockMovement()
    {
    }

    private StockMovement(StockAdjustment adjustment, long quantityAfter)
    {
        Id = adjustment.Id;
        OrganizationId = adjustment.OrganizationId;
        WarehouseId = adjustment.WarehouseId;
        ProductId = adjustment.ProductId;
        PerformedByUserId = adjustment.PerformedByUserId;
        QuantityDelta = adjustment.QuantityDelta;
        QuantityAfter = quantityAfter;
        Reason = adjustment.Reason;
        CreatedAtUtc = adjustment.CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public long QuantityDelta { get; private set; }

    public long QuantityAfter { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static StockMovement Create(StockAdjustment adjustment, long quantityAfter)
    {
        return new StockMovement(adjustment, quantityAfter);
    }
}
