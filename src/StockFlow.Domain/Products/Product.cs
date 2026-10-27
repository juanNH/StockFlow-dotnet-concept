namespace StockFlow.Domain.Products;

public sealed class Product
{
    public const int MaxSkuLength = 64;
    public const int MaxNameLength = 120;
    public const int MaxDescriptionLength = 500;
    public const int CurrencyLength = 3;
    public const int PriceScale = 2;
    public const decimal MaxUnitPrice = 9_999_999_999_999_999.99m;

    private Product()
    {
    }

    private Product(
        Guid id,
        Guid organizationId,
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        string currency,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Sku = sku;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        Currency = currency;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal UnitPrice { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Product Create(
        Guid organizationId,
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        string currency,
        DateTimeOffset createdAtUtc)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ProductRuleViolationException("Organization is required.");
        }

        var normalizedSku = NormalizeSku(sku);
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);
        var normalizedCurrency = NormalizeCurrency(currency);
        ValidateUnitPrice(unitPrice);
        var timestamp = createdAtUtc.ToUniversalTime();

        return new Product(
            Guid.NewGuid(),
            organizationId,
            normalizedSku,
            normalizedName,
            normalizedDescription,
            unitPrice,
            normalizedCurrency,
            timestamp);
    }

    public void Update(
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        string currency,
        bool isActive,
        DateTimeOffset updatedAtUtc)
    {
        var normalizedSku = NormalizeSku(sku);
        var normalizedName = NormalizeName(name);
        var normalizedDescription = NormalizeDescription(description);
        ValidateUnitPrice(unitPrice);
        var normalizedCurrency = NormalizeCurrency(currency);
        var timestamp = updatedAtUtc.ToUniversalTime();

        Sku = normalizedSku;
        Name = normalizedName;
        Description = normalizedDescription;
        UnitPrice = unitPrice;
        Currency = normalizedCurrency;
        IsActive = isActive;
        UpdatedAtUtc = timestamp;
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    private static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ProductRuleViolationException("SKU is required.");
        }

        var normalizedSku = sku.Trim().ToUpperInvariant();
        if (normalizedSku.Length > MaxSkuLength || !IsValidSku(normalizedSku))
        {
            throw new ProductRuleViolationException(
                $"SKU must be 1 to {MaxSkuLength} letters or digits separated by single hyphens.");
        }

        return normalizedSku;
    }

    private static bool IsValidSku(string sku)
    {
        if (sku.Length == 0 || !char.IsAsciiLetterOrDigit(sku[0]) || !char.IsAsciiLetterOrDigit(sku[^1]))
        {
            return false;
        }

        var previousWasHyphen = false;
        foreach (var character in sku)
        {
            if (character == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
                continue;
            }

            if (!char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }

            previousWasHyphen = false;
        }

        return true;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductRuleViolationException("Product name is required.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new ProductRuleViolationException($"Product name cannot exceed {MaxNameLength} characters.");
        }

        return normalizedName;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > MaxDescriptionLength)
        {
            throw new ProductRuleViolationException(
                $"Product description cannot exceed {MaxDescriptionLength} characters.");
        }

        return normalizedDescription;
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ProductRuleViolationException("Currency is required.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != CurrencyLength
            || normalizedCurrency.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new ProductRuleViolationException("Currency must be a three-letter code.");
        }

        return normalizedCurrency;
    }

    private static void ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0 || unitPrice > MaxUnitPrice || decimal.Round(unitPrice, PriceScale) != unitPrice)
        {
            throw new ProductRuleViolationException(
                $"Unit price must be greater than zero and have no more than {PriceScale} decimal places.");
        }
    }
}
