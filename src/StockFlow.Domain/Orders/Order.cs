namespace StockFlow.Domain.Orders;

public sealed class Order
{
    public const int MaxLineCount = 50;
    public const int MaxIdempotencyKeyLength = 128;
    public const int RequestFingerprintLength = 64;
    public const int ReservationLifetimeMinutes = 2;

    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        Guid organizationId,
        Guid warehouseId,
        Guid createdByUserId,
        string idempotencyKey,
        string requestFingerprint,
        OrderStatus status,
        string currency,
        decimal totalAmount,
        DateTimeOffset createdAtUtc,
        DateTimeOffset reservationExpiresAtUtc,
        DateTimeOffset? expiredAtUtc,
        IEnumerable<OrderLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        WarehouseId = warehouseId;
        CreatedByUserId = createdByUserId;
        IdempotencyKey = idempotencyKey;
        RequestFingerprint = requestFingerprint;
        Status = status;
        Currency = currency;
        TotalAmount = totalAmount;
        CreatedAtUtc = createdAtUtc;
        ReservationExpiresAtUtc = reservationExpiresAtUtc;
        ExpiredAtUtc = expiredAtUtc;
        _lines.AddRange(lines);
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string RequestFingerprint { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ReservationExpiresAtUtc { get; private set; }

    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Reserve(
        Guid id,
        Guid organizationId,
        Guid warehouseId,
        Guid createdByUserId,
        string idempotencyKey,
        string requestFingerprint,
        string currency,
        DateTimeOffset createdAtUtc,
        DateTimeOffset reservationExpiresAtUtc,
        IEnumerable<OrderLine> lines)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || warehouseId == Guid.Empty || createdByUserId == Guid.Empty)
        {
            throw new OrderRuleViolationException("Order, organization, warehouse, and creator are required.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length > MaxIdempotencyKeyLength
            || idempotencyKey.Any(character => !char.IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '~' and not '-'))
        {
            throw new OrderRuleViolationException("Idempotency-Key must be 1 to 128 ASCII letters, digits, '.', '_', '~' or '-'.");
        }

        if (requestFingerprint.Length != RequestFingerprintLength
            || requestFingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new OrderRuleViolationException("The order request fingerprint is invalid.");
        }

        if (string.IsNullOrWhiteSpace(currency)
            || currency.Length != 3
            || currency.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new OrderRuleViolationException("Order currency must be a three-letter code.");
        }

        var normalizedCurrency = currency.ToUpperInvariant();
        var normalizedLines = lines.OrderBy(line => line.ProductId).ToArray();
        if (normalizedLines.Length is < 1 or > MaxLineCount)
        {
            throw new OrderRuleViolationException($"An order must contain 1 to {MaxLineCount} distinct product lines.");
        }

        if (normalizedLines.Any(line => line.OrganizationId != organizationId || line.OrderId != id))
        {
            throw new OrderRuleViolationException("All order lines must belong to the order organization and ID.");
        }

        if (normalizedLines.Select(line => line.ProductId).Distinct().Count() != normalizedLines.Length)
        {
            throw new OrderRuleViolationException("An order can contain each product only once.");
        }

        if (normalizedLines.Any(line => !string.Equals(line.Currency, normalizedCurrency, StringComparison.Ordinal)))
        {
            throw new OrderCurrencyMismatchException();
        }

        var totalAmount = normalizedLines.Sum(line => line.LineTotal);
        if (totalAmount <= 0 || decimal.Round(totalAmount, 2) != totalAmount)
        {
            throw new OrderRuleViolationException("Order total must be positive and have no more than two decimal places.");
        }

        var createdAt = createdAtUtc.ToUniversalTime();
        var reservationExpiresAt = reservationExpiresAtUtc.ToUniversalTime();
        if (reservationExpiresAt <= createdAt)
        {
            throw new OrderRuleViolationException("Reservation expiration must be after order creation.");
        }

        return new Order(
            id,
            organizationId,
            warehouseId,
            createdByUserId,
            idempotencyKey,
            requestFingerprint.ToUpperInvariant(),
            OrderStatus.Reserved,
            normalizedCurrency,
            totalAmount,
            createdAt,
            reservationExpiresAt,
            expiredAtUtc: null,
            normalizedLines);
    }

    public static Order Restore(
        Guid id,
        Guid organizationId,
        Guid warehouseId,
        Guid createdByUserId,
        string idempotencyKey,
        string requestFingerprint,
        OrderStatus status,
        string currency,
        decimal totalAmount,
        DateTimeOffset createdAtUtc,
        DateTimeOffset reservationExpiresAtUtc,
        DateTimeOffset? expiredAtUtc,
        IEnumerable<OrderLine> lines)
    {
        return new Order(
            id,
            organizationId,
            warehouseId,
            createdByUserId,
            idempotencyKey,
            requestFingerprint,
            status,
            currency,
            totalAmount,
            createdAtUtc,
            reservationExpiresAtUtc,
            expiredAtUtc,
            lines);
    }

    public bool Expire(DateTimeOffset expiredAtUtc)
    {
        if (Status == OrderStatus.Expired)
        {
            return false;
        }

        if (Status != OrderStatus.Reserved)
        {
            throw new OrderInvalidStateException();
        }

        var timestamp = expiredAtUtc.ToUniversalTime();
        if (timestamp < ReservationExpiresAtUtc)
        {
            throw new OrderReservationNotDueException();
        }

        Status = OrderStatus.Expired;
        ExpiredAtUtc = timestamp;
        return true;
    }

    public Order ToCreationSnapshot()
    {
        return Restore(
            Id,
            OrganizationId,
            WarehouseId,
            CreatedByUserId,
            IdempotencyKey,
            RequestFingerprint,
            OrderStatus.Reserved,
            Currency,
            TotalAmount,
            CreatedAtUtc,
            ReservationExpiresAtUtc,
            expiredAtUtc: null,
            _lines);
    }
}
