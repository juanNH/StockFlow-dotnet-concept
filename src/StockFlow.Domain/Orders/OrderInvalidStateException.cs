namespace StockFlow.Domain.Orders;

public sealed class OrderInvalidStateException()
    : Exception("The order cannot make the requested state transition.");
