using Winnow.API.Domain.Common;
using Winnow.API.Domain.Core;
using Winnow.API.Domain.Organizations.Events;

namespace Winnow.API.Domain.Organizations;

/// <summary>
/// Represents an invitation for a user to join an organization.
/// </summary>
public class OrganizationInvitation : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Email Email { get; private set; }
    public Guid RoleId { get; private set; }
    public Winnow.API.Domain.Security.Role Role { get; private set; } = null!;
    public string Token { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private readonly List<Guid> _initialTeamIds = [];
    public IReadOnlyCollection<Guid> InitialTeamIds => _initialTeamIds.AsReadOnly();

    private readonly List<Guid> _initialProjectIds = [];
    public IReadOnlyCollection<Guid> InitialProjectIds => _initialProjectIds.AsReadOnly();

    public bool IsAccepted { get; private set; }
    public bool IsRevoked { get; private set; }

    // Private EF constructor
    private OrganizationInvitation()
    {
        Token = null!;
    }

    public OrganizationInvitation(
        Guid organizationId,
        Email email,
        Guid roleId,
        string token,
        IEnumerable<Guid>? initialTeamIds = null,
        IEnumerable<Guid>? initialProjectIds = null,
        int expiresAfterHours = 24)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role is required.", nameof(roleId));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Email = email;
        RoleId = roleId;
        Token = token;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddHours(expiresAfterHours);

        if (initialTeamIds != null)
            _initialTeamIds.AddRange(initialTeamIds);
        if (initialProjectIds != null)
            _initialProjectIds.AddRange(initialProjectIds);

        _domainEvents.Add(new OrganizationInvitationCreatedEvent(Id, OrganizationId, Email.Value, Token));
    }

    public void Accept()
    {
        if (IsExpired())
            throw new InvalidOperationException("Invitation has expired.");
        if (IsRevoked)
            throw new InvalidOperationException("Invitation has been revoked.");
        if (IsAccepted)
            return;

        IsAccepted = true;
        _domainEvents.Add(new OrganizationInvitationAcceptedEvent(Id, OrganizationId, Email.Value));
    }

    public void Resend(string newToken, int expiresAfterHours = 24)
    {
        if (IsAccepted)
            throw new InvalidOperationException("Cannot resend an invitation that has already been accepted.");

        Token = newToken;
        ExpiresAt = DateTime.UtcNow.AddHours(expiresAfterHours);

        // You might want a domain event here too
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        _domainEvents.Add(new OrganizationInvitationRevokedEvent(Id, OrganizationId));
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
