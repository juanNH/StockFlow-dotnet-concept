namespace StockFlow.Domain.Inventory;

public sealed class InventoryBalance
{
    private InventoryBalance()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public long QuantityOnHand { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }
}
