using System.Text;

namespace StockFlow.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 20;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Authentication:Jwt:Issuer and Audience are required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        if (AccessTokenLifetimeMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:AccessTokenLifetimeMinutes must be between 5 and 60.");
        }
    }
}
