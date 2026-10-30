namespace StockFlow.Domain.Orders;

public sealed class OrderRuleViolationException(string message) : Exception(message);
