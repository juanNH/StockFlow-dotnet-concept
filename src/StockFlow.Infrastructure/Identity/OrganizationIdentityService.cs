using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using StockFlow.Application.Abstractions;
using StockFlow.Application.Identity;
using StockFlow.Domain.Organizations;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Infrastructure.Identity;

internal sealed class OrganizationIdentityService(
    StockFlowDbContext dbContext,
    UserManager<StockFlowUser> userManager,
    JwtAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IOrganizationIdentityService
{
    public async Task<OrganizationIdentitySession> RegisterAsync(
        string organizationName,
        string organizationSlug,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = email.Trim();
        var normalizedSlug = organizationSlug.Trim().ToLowerInvariant();
        var organization = Organization.Create(organizationName, normalizedSlug, timeProvider.GetUtcNow());
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new StockFlowUser
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = false
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                if (createResult.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                {
                    throw new IdentityConflictException();
                }

                throw new IdentityValidationException();
            }

            var membership = OrganizationMembership.Create(
                user.Id,
                organization.Id,
                OrganizationRole.Owner,
                timeProvider.GetUtcNow());

            dbContext.Organizations.Add(organization);
            dbContext.OrganizationMemberships.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreateSession(user.Id, normalizedEmail, organization, membership.Role);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await RollbackQuietlyAsync(transaction, cancellationToken);
            throw new IdentityConflictException();
        }
    }

    public async Task<OrganizationIdentitySession?> LoginAsync(
        string email,
        string password,
        string organizationSlug,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = email.Trim();
        var normalizedSlug = organizationSlug.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            DummyPasswordVerifier.Verify(password);
            return null;
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
        {
            return null;
        }

        var membership = await (
            from item in dbContext.OrganizationMemberships
            join organization in dbContext.Organizations on item.OrganizationId equals organization.Id
            where item.UserId == user.Id && organization.Slug == normalizedSlug
            select new { Membership = item, Organization = organization })
            .SingleOrDefaultAsync(cancellationToken);
        if (membership is null)
        {
            return null;
        }

        return CreateSession(
            user.Id,
            user.Email ?? normalizedEmail,
            membership.Organization,
            membership.Membership.Role);
    }

    private OrganizationIdentitySession CreateSession(
        Guid userId,
        string email,
        Organization organization,
        OrganizationRole role)
    {
        var (token, expiresAtUtc) = tokenIssuer.Issue(userId, email, organization, role);
        return new OrganizationIdentitySession(
            token,
            expiresAtUtc,
            userId,
            email,
            organization.Id,
            organization.Name,
            organization.Slug,
            role.ToString());
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }

    private async Task RollbackQuietlyAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The transaction is also rolled back when disposed.
        }
    }
}
