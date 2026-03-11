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
    public string Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsLocked { get; private set; }

    // Private EF constructor
    private OrganizationMember()
    {
        UserId = null!;
        Role = null!;
    }

    public OrganizationMember(Guid organizationId, string userId, string role = "Member")
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
        IsLocked = false;

        _domainEvents.Add(new OrganizationMemberJoinedEvent(Id, OrganizationId, UserId, Role));
    }

    public void ChangeRole(string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            throw new ArgumentException("Role is required.", nameof(newRole));
        if (Role == newRole) return;

        var oldRole = Role;
        Role = newRole;
        _domainEvents.Add(new OrganizationMemberRoleChangedEvent(Id, OrganizationId, oldRole, newRole));
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

    public bool IsAdmin()
    {
        return string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Role, "owner", StringComparison.OrdinalIgnoreCase);
    }
}
