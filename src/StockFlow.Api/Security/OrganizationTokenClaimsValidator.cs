using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using StockFlow.Application.Identity;
using StockFlow.Domain.Organizations;

namespace StockFlow.Api.Security;

public static class OrganizationTokenClaimsValidator
{
    public static bool IsValid(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userId = principal.FindFirst(IdentityClaimTypes.UserId)?.Value;
        var email = principal.FindFirst(IdentityClaimTypes.Email)?.Value;
        var organizationId = principal.FindFirst(IdentityClaimTypes.OrganizationId)?.Value;
        var organizationName = principal.FindFirst(IdentityClaimTypes.OrganizationName)?.Value;
        var organizationSlug = principal.FindFirst(IdentityClaimTypes.OrganizationSlug)?.Value;
        var organizationRole = principal.FindFirst(IdentityClaimTypes.OrganizationRole)?.Value;

        return Guid.TryParse(userId, out var parsedUserId)
            && parsedUserId != Guid.Empty
            && !string.IsNullOrWhiteSpace(email)
            && new EmailAddressAttribute().IsValid(email)
            && Guid.TryParse(organizationId, out var parsedOrganizationId)
            && parsedOrganizationId != Guid.Empty
            && !string.IsNullOrWhiteSpace(organizationName)
            && !string.IsNullOrWhiteSpace(organizationSlug)
            && Regex.IsMatch(organizationSlug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)
            && organizationSlug.Length <= 80
            && Enum.TryParse<OrganizationRole>(organizationRole, ignoreCase: false, out var role)
            && Enum.IsDefined(role);
    }
}
