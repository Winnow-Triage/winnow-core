using Winnow.Server.Domain.Events;

namespace Winnow.Server.Domain.Aggregates;

/// <summary>
/// Represents a Winnow project that receives reports from a client SDK.
/// A Project has a primary API key and can issue a secondary key during rotation.
/// Projects optionally belong to a Team within an Organization.
/// </summary>
public class Project : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Team membership is optional
    public Guid? TeamId { get; private set; }

    // ──────────────────────────────────────────────────────────────
    // API key management
    // Hashes are stored — actual key generation is the caller's responsibility.
    // ──────────────────────────────────────────────────────────────

    /// <summary>The SHA-256 hash of the active primary API key.</summary>
    public string ApiKeyHash { get; private set; }

    /// <summary>
    /// Hash of the secondary key issued during rotation.
    /// Both primary and secondary keys are valid until the secondary expires
    /// or is promoted (RotateToPrimary is called).
    /// </summary>
    public string? SecondaryApiKeyHash { get; private set; }
    public DateTimeOffset? SecondaryApiKeyExpiresAt { get; private set; }

    // Private EF constructor
    private Project()
    {
        Name = null!;
        OwnerId = null!;
        ApiKeyHash = null!;
    }

    public Project(
        Guid organizationId,
        string name,
        string ownerId,
        string initialApiKeyHash)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(initialApiKeyHash))
            throw new ArgumentException("An initial API key hash is required.", nameof(initialApiKeyHash));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name;
        OwnerId = ownerId;
        ApiKeyHash = initialApiKeyHash;
        CreatedAt = DateTime.UtcNow;

        _domainEvents.Add(new ProjectCreatedEvent(Id, OrganizationId, OwnerId));
    }

    // ──────────────────────────────────────────────────────────────
    // API key rotation
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Issues a secondary API key with a limited validity window.
    /// Both keys will be accepted until the secondary expires or is promoted.
    /// </summary>
    public void IssueSecondaryApiKey(string secondaryKeyHash, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(secondaryKeyHash))
            throw new ArgumentException("Secondary key hash is required.", nameof(secondaryKeyHash));
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Secondary key expiry must be in the future.", nameof(expiresAt));

        SecondaryApiKeyHash = secondaryKeyHash;
        SecondaryApiKeyExpiresAt = expiresAt;
    }

    /// <summary>
    /// Promotes the secondary key to primary, invalidating the old primary.
    /// Use this once the client SDK has been updated to use the new key.
    /// </summary>
    public void RotateToPrimary()
    {
        if (SecondaryApiKeyHash is null)
            throw new InvalidOperationException("No secondary key has been issued to rotate to.");

        ApiKeyHash = SecondaryApiKeyHash;
        SecondaryApiKeyHash = null;
        SecondaryApiKeyExpiresAt = null;

        _domainEvents.Add(new ProjectApiKeyRotatedEvent(Id, OrganizationId));
    }

    // ──────────────────────────────────────────────────────────────
    // Team membership
    // ──────────────────────────────────────────────────────────────

    public void AssignToTeam(Guid teamId)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("Team ID must not be empty.", nameof(teamId));
        if (TeamId == teamId) return; // idempotent

        TeamId = teamId;
        _domainEvents.Add(new ProjectTeamAssignedEvent(Id, teamId));
    }

    public void RemoveFromTeam() => TeamId = null;

    // ──────────────────────────────────────────────────────────────
    // Basic mutations
    // ──────────────────────────────────────────────────────────────

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Project name is required.", nameof(newName));
        Name = newName;
    }
}
