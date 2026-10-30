namespace StockFlow.Domain.Orders;

public sealed class OrderCurrencyMismatchException()
    : Exception("All products in an order must use the same currency.");
