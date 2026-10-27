using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Identity;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IOrganizationIdentityService identityService,
    ICurrentTenant currentTenant) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(OrganizationIdentitySession), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<OrganizationIdentitySession>> Register(
        [FromBody] RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var session = await identityService.RegisterAsync(
            request.OrganizationName,
            request.OrganizationSlug,
            request.Email,
            request.Password,
            cancellationToken);

        return CreatedAtAction(nameof(GetCurrentIdentity), session);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(OrganizationIdentitySession), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OrganizationIdentitySession>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await identityService.LoginAsync(
            request.Email,
            request.Password,
            request.OrganizationSlug,
            cancellationToken);

        if (session is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email, password, or organization slug is invalid.");
        }

        return Ok(session);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentIdentityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentIdentityResponse> GetCurrentIdentity()
    {
        return Ok(new CurrentIdentityResponse(
            currentTenant.UserId,
            currentTenant.Email,
            currentTenant.OrganizationId,
            currentTenant.OrganizationName,
            currentTenant.OrganizationSlug,
            currentTenant.Role.ToString()));
    }
}

public sealed record RegisterOrganizationRequest(
    [param: Required, StringLength(120, MinimumLength = 2)] string OrganizationName,
    [param: Required, StringLength(80), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")] string OrganizationSlug,
    [param: Required, EmailAddress, StringLength(256)] string Email,
    [param: Required, StringLength(IdentityPasswordPolicy.MaximumLength, MinimumLength = IdentityPasswordPolicy.MinimumLength), RegularExpression(IdentityPasswordPolicy.Pattern)] string Password);

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(256)] string Email,
    [param: Required, StringLength(128, MinimumLength = 1)] string Password,
    [param: Required, StringLength(80), RegularExpression("^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$")] string OrganizationSlug);

public sealed record CurrentIdentityResponse(
    Guid UserId,
    string Email,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string Role);
