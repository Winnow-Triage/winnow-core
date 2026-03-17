using Winnow.Server.Domain.Organizations;

namespace Winnow.Server.Domain.Security;

public class OrganizationUserRole
{
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;

    public string UserId { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    private OrganizationUserRole() { }

    public OrganizationUserRole(Guid organizationId, string userId, Guid roleId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID is required.", nameof(roleId));

        OrganizationId = organizationId;
        UserId = userId;
        RoleId = roleId;
    }
}
