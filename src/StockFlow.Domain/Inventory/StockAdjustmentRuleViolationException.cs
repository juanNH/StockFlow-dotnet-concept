namespace StockFlow.Domain.Inventory;

public sealed class StockAdjustmentRuleViolationException(string message) : Exception(message);
