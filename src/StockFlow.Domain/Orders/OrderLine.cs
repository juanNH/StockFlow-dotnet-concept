namespace StockFlow.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    private OrderLine(
        Guid organizationId,
        Guid orderId,
        Guid productId,
        string productSku,
        string productName,
        long quantity,
        decimal unitPrice,
        string currency,
        decimal lineTotal)
    {
        OrganizationId = organizationId;
        OrderId = orderId;
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Currency = currency;
        LineTotal = lineTotal;
    }

    public Guid OrganizationId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public long Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal LineTotal { get; private set; }

    public static OrderLine Create(
        Guid organizationId,
        Guid orderId,
        Guid productId,
        string productSku,
        string productName,
        long quantity,
        decimal unitPrice,
        string currency)
    {
        if (organizationId == Guid.Empty || orderId == Guid.Empty || productId == Guid.Empty)
        {
            throw new OrderRuleViolationException("Organization, order, and product are required for an order line.");
        }

        if (string.IsNullOrWhiteSpace(productSku) || productSku.Length > 64
            || string.IsNullOrWhiteSpace(productName) || productName.Length > 120)
        {
            throw new OrderRuleViolationException("Order product snapshots are invalid.");
        }

        if (quantity is < 1 or > 1_000_000)
        {
            throw new OrderRuleViolationException("Order line quantity must be between 1 and 1,000,000.");
        }

        if (unitPrice <= 0 || decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new OrderRuleViolationException("Order line unit price must be positive with no more than two decimal places.");
        }

        if (string.IsNullOrWhiteSpace(currency)
            || currency.Length != 3
            || currency.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new OrderRuleViolationException("Order line currency must be a three-letter code.");
        }

        var normalizedCurrency = currency.ToUpperInvariant();
        var lineTotal = unitPrice * quantity;
        if (decimal.Round(lineTotal, 2) != lineTotal)
        {
            throw new OrderRuleViolationException("Order line total must have no more than two decimal places.");
        }

        return new OrderLine(
            organizationId,
            orderId,
            productId,
            productSku,
            productName,
            quantity,
            unitPrice,
            normalizedCurrency,
            lineTotal);
    }

    public static OrderLine Restore(
        Guid organizationId,
        Guid orderId,
        Guid productId,
        string productSku,
        string productName,
        long quantity,
        decimal unitPrice,
        string currency,
        decimal lineTotal)
    {
        return new OrderLine(
            organizationId,
            orderId,
            productId,
            productSku,
            productName,
            quantity,
            unitPrice,
            currency,
            lineTotal);
    }
}
