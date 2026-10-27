namespace StockFlow.Domain.Products;

public sealed class ProductRuleViolationException(string message) : Exception(message);
