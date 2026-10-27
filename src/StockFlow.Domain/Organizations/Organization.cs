namespace StockFlow.Domain.Organizations;

public sealed class Organization
{
    public const int MaxNameLength = 120;
    public const int MaxSlugLength = 80;

    private Organization()
    {
    }

    private Organization(Guid id, string name, string slug, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Organization Create(string name, string slug, DateTimeOffset createdAtUtc)
    {
        var normalizedName = NormalizeName(name);
        var normalizedSlug = NormalizeSlug(slug);

        return new Organization(Guid.NewGuid(), normalizedName, normalizedSlug, createdAtUtc.ToUniversalTime());
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Organization name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Organization name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        return normalizedName;
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Organization slug is required.", nameof(slug));
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length > MaxSlugLength || !IsValidSlug(normalizedSlug))
        {
            throw new ArgumentException(
                $"Organization slug must be 1 to {MaxSlugLength} lowercase letters or digits separated by single hyphens.",
                nameof(slug));
        }

        return normalizedSlug;
    }

    private static bool IsValidSlug(string slug)
    {
        if (slug.Length == 0 || slug[0] == '-' || slug[^1] == '-')
        {
            return false;
        }

        var previousWasHyphen = false;
        foreach (var character in slug)
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
}
