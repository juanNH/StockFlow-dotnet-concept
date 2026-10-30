using StockFlow.Application.Abstractions;

namespace StockFlow.Application.Orders;

public sealed class GetOrderHandler(IOrderRepository orders, ICurrentTenant currentTenant)
{
    public async Task<OrderResponse> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            throw new OrderNotFoundException();
        }

        var order = await orders.GetByIdAsync(currentTenant.OrganizationId, orderId, cancellationToken);
        return order is null ? throw new OrderNotFoundException() : OrderResponse.From(order);
    }
}
