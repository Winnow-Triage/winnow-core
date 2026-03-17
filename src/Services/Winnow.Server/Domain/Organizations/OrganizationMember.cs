using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Organizations.Events;

namespace Winnow.Server.Domain.Organizations;

/// <summary>
/// Represents a membership of a user within an organization.
/// </summary>
public class OrganizationMember : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid RoleId { get; private set; }
    public Winnow.Server.Domain.Security.Role Role { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }
    public bool IsLocked { get; private set; }

    // Private EF constructor
    private OrganizationMember()
    {
        UserId = null!;
    }

    public OrganizationMember(Guid organizationId, string userId, Guid roleId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role is required.", nameof(roleId));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        UserId = userId;
        RoleId = roleId;
        JoinedAt = DateTime.UtcNow;
        IsLocked = false;

        _domainEvents.Add(new OrganizationMemberJoinedEvent(Id, OrganizationId, UserId, RoleId.ToString()));
    }

    public void ChangeRole(Guid newRoleId)
    {
        if (newRoleId == Guid.Empty)
            throw new ArgumentException("Role is required.", nameof(newRoleId));
        if (RoleId == newRoleId) return;

        var oldRole = RoleId;
        RoleId = newRoleId;
        _domainEvents.Add(new OrganizationMemberRoleChangedEvent(Id, OrganizationId, oldRole.ToString(), newRoleId.ToString()));
    }

    public void Lock()
    {
        if (IsLocked) return;
        IsLocked = true;
        _domainEvents.Add(new OrganizationMemberLockedEvent(Id, OrganizationId));
    }

    public void Unlock()
    {
        if (!IsLocked) return;
        IsLocked = false;
        _domainEvents.Add(new OrganizationMemberUnlockedEvent(Id, OrganizationId));
    }

    // Role checking will now occur via the AuthorizeBehavior and RolePermission mappings.
    // IsAdmin() is deprecated for RBAC.
}
