using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockFlow.Application.Identity;
using StockFlow.Domain.Organizations;

namespace StockFlow.Infrastructure.Identity;

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAtUtc) Issue(
        Guid userId,
        string email,
        Organization organization,
        OrganizationRole role)
    {
        var issuedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim(IdentityClaimTypes.UserId, userId.ToString()),
            new Claim(IdentityClaimTypes.Email, email),
            new Claim(IdentityClaimTypes.OrganizationId, organization.Id.ToString()),
            new Claim(IdentityClaimTypes.OrganizationName, organization.Name),
            new Claim(IdentityClaimTypes.OrganizationSlug, organization.Slug),
            new Claim(IdentityClaimTypes.OrganizationRole, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
