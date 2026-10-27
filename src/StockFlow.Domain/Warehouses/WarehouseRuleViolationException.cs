namespace StockFlow.Domain.Warehouses;

public sealed class WarehouseRuleViolationException(string message) : Exception(message);
