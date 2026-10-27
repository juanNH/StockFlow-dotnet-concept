using StockFlow.Domain.Organizations;

namespace StockFlow.Application.Abstractions;

public interface ICurrentTenant
{
    Guid UserId { get; }

    Guid OrganizationId { get; }

    OrganizationRole Role { get; }

    string Email { get; }

    string OrganizationName { get; }

    string OrganizationSlug { get; }
}
