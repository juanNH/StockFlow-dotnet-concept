namespace StockFlow.Domain.Orders;

public sealed class OrderReservationNotDueException()
    : Exception("The order reservation has not expired yet.");
