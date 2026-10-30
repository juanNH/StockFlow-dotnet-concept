using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StockFlow.Application.Abstractions;
using StockFlow.Domain.Orders;

namespace StockFlow.Application.Orders;

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(Order.ReservationLifetimeMinutes);

    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (command.WarehouseId == Guid.Empty)
        {
            throw new OrderRuleViolationException("Warehouse is required.");
        }

        if (command.Lines is null || command.Lines.Count is < 1 or > Order.MaxLineCount)
        {
            throw new OrderRuleViolationException($"An order must contain 1 to {Order.MaxLineCount} product lines.");
        }

        var normalizedLines = NormalizeLines(command.Lines);
        if (normalizedLines.Count > Order.MaxLineCount)
        {
            throw new OrderRuleViolationException($"An order can contain at most {Order.MaxLineCount} distinct product lines.");
        }

        var requestFingerprint = CalculateFingerprint(command.WarehouseId, normalizedLines);
        var createdAtUtc = timeProvider.GetUtcNow();
        var request = new CreateOrderPersistenceRequest(
            currentTenant.OrganizationId,
            currentTenant.UserId,
            command.WarehouseId,
            command.IdempotencyKey,
            requestFingerprint,
            normalizedLines,
            createdAtUtc,
            createdAtUtc.Add(ReservationLifetime));

        var result = await orders.CreateAndReserveAsync(request, cancellationToken);
        return new CreateOrderResult(OrderResponse.From(result.Order), result.WasReplay);
    }

    private static IReadOnlyList<OrderLineRequest> NormalizeLines(IReadOnlyList<OrderLineRequest> lines)
    {
        if (lines.Any(line => line.ProductId == Guid.Empty || line.Quantity <= 0))
        {
            throw new OrderRuleViolationException("Each order line requires a product and a positive quantity.");
        }

        try
        {
            var normalized = lines
                .GroupBy(line => line.ProductId)
                .Select(group => new OrderLineRequest(group.Key, checked(group.Sum(line => line.Quantity))))
                .OrderBy(line => line.ProductId)
                .ToArray();

            if (normalized.Any(line => line.Quantity > 1_000_000))
            {
                throw new OrderRuleViolationException("Order line quantity cannot exceed 1,000,000.");
            }

            return normalized;
        }
        catch (OverflowException)
        {
            throw new OrderRuleViolationException("The combined order line quantity is too large.");
        }
    }

    private static string CalculateFingerprint(Guid warehouseId, IReadOnlyList<OrderLineRequest> lines)
    {
        var canonicalRequest = JsonSerializer.Serialize(new
        {
            WarehouseId = warehouseId,
            Lines = lines.Select(line => new { line.ProductId, line.Quantity })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    }
}
