using StockFlow.Application.Abstractions;
using StockFlow.Domain.Orders;

namespace StockFlow.Application.Orders;

public sealed class ExpireDueReservationsHandler(
    IOrderRepository orders,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    public async Task<ReservationExpirationResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await orders.ExpireDueReservationsAsync(
            currentTenant.OrganizationId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        return ReservationExpirationResponse.From(result);
    }
}
