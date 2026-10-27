namespace StockFlow.Application.Abstractions;

public interface IOrganizationIdentityService
{
    Task<OrganizationIdentitySession> RegisterAsync(
        string organizationName,
        string organizationSlug,
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<OrganizationIdentitySession?> LoginAsync(
        string email,
        string password,
        string organizationSlug,
        CancellationToken cancellationToken);
}

public sealed record OrganizationIdentitySession(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string Email,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string Role)
{
    public string TokenType => "Bearer";
}
