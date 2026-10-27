namespace StockFlow.Domain.Organizations;

public sealed class OrganizationMembership
{
    private OrganizationMembership()
    {
    }

    private OrganizationMembership(
        Guid id,
        Guid userId,
        Guid organizationId,
        OrganizationRole role,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public OrganizationRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static OrganizationMembership Create(
        Guid userId,
        Guid organizationId,
        OrganizationRole role,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Organization role is not supported.");
        }

        return new OrganizationMembership(
            Guid.NewGuid(),
            userId,
            organizationId,
            role,
            createdAtUtc.ToUniversalTime());
    }
}
