namespace StockFlow.Application.Orders;

public sealed class OrderNotFoundException()
    : Exception("The order was not found in the current organization.");

public sealed class OrderIdempotencyConflictException()
    : Exception("The Idempotency-Key was already used with a different order request.");

public sealed class OrderInsufficientStockException()
    : Exception("There is not enough available stock to reserve every order line.");

public sealed class OrderProductInactiveException()
    : Exception("An inactive product cannot be added to an order.");

public sealed class OrderMembershipRequiredException()
    : Exception("The order creator must be a member of the current organization.");

public sealed class OrderStockQuantityOutOfRangeException()
    : Exception("The resulting reserved stock quantity is outside the supported range.");
