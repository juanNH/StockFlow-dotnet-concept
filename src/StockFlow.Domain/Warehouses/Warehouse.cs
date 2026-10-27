namespace StockFlow.Domain.Warehouses;

public sealed class Warehouse
{
    public const int MaxCodeLength = 32;
    public const int MaxNameLength = 120;

    private Warehouse()
    {
    }

    private Warehouse(Guid id, Guid organizationId, string code, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Warehouse Create(
        Guid organizationId,
        string code,
        string name,
        DateTimeOffset createdAtUtc)
    {
        if (organizationId == Guid.Empty)
        {
            throw new WarehouseRuleViolationException("Organization is required.");
        }

        var normalizedCode = NormalizeCode(code);
        var normalizedName = NormalizeName(name);
        return new Warehouse(
            Guid.NewGuid(),
            organizationId,
            normalizedCode,
            normalizedName,
            createdAtUtc.ToUniversalTime());
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new WarehouseRuleViolationException("Warehouse code is required.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length > MaxCodeLength || !IsValidCode(normalizedCode))
        {
            throw new WarehouseRuleViolationException(
                $"Warehouse code must be 1 to {MaxCodeLength} letters or digits separated by single hyphens.");
        }

        return normalizedCode;
    }

    private static bool IsValidCode(string code)
    {
        if (code.Length == 0 || !char.IsAsciiLetterOrDigit(code[0]) || !char.IsAsciiLetterOrDigit(code[^1]))
        {
            return false;
        }

        var previousWasHyphen = false;
        foreach (var character in code)
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
            throw new WarehouseRuleViolationException("Warehouse name is required.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new WarehouseRuleViolationException($"Warehouse name cannot exceed {MaxNameLength} characters.");
        }

        return normalizedName;
    }
}
