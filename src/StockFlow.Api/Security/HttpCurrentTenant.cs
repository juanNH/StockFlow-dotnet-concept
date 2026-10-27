using System.Security.Claims;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Identity;
using StockFlow.Domain.Organizations;

namespace StockFlow.Api.Security;

public sealed class HttpCurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public Guid UserId => ReadGuid(IdentityClaimTypes.UserId);

    public Guid OrganizationId => ReadGuid(IdentityClaimTypes.OrganizationId);

    public OrganizationRole Role => ReadRole();

    public string Email => ReadRequiredClaim(IdentityClaimTypes.Email);

    public string OrganizationName => ReadRequiredClaim(IdentityClaimTypes.OrganizationName);

    public string OrganizationSlug => ReadRequiredClaim(IdentityClaimTypes.OrganizationSlug);

    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("The current request does not have an authenticated user.");

    private Guid ReadGuid(string claimType)
    {
        var value = ReadRequiredClaim(claimType);
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        throw new InvalidOperationException($"The authenticated token does not contain a valid {claimType} claim.");
    }

    private string ReadRequiredClaim(string claimType)
    {
        return Principal.FindFirstValue(claimType)
            ?? throw new InvalidOperationException($"The authenticated token is missing the {claimType} claim.");
    }

    private OrganizationRole ReadRole()
    {
        var value = ReadRequiredClaim(IdentityClaimTypes.OrganizationRole);
        if (Enum.TryParse<OrganizationRole>(value, ignoreCase: false, out var role) && Enum.IsDefined(role))
        {
            return role;
        }

        throw new InvalidOperationException("The authenticated token does not contain a supported organization role.");
    }
}
