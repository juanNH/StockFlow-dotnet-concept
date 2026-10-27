namespace StockFlow.Application.Inventory;

public sealed record AdjustStockCommand(Guid WarehouseId, Guid ProductId, long QuantityDelta, string Reason);
