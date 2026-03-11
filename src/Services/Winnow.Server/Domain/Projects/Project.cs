using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Projects.Events;
using Winnow.Server.Domain.Teams;

namespace Winnow.Server.Domain.Projects;

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
    private readonly List<Guid> _integrationIds = [];
    public IReadOnlyCollection<Guid> Integrations => _integrationIds.AsReadOnly();

    private readonly List<Guid> _clusterIds = [];
    public IReadOnlyCollection<Guid> Clusters => _clusterIds.AsReadOnly();

    private readonly List<Guid> _reportIds = [];
    public IReadOnlyCollection<Guid> Reports => _reportIds.AsReadOnly();

    // EF Core Navigation Properties - Private/Internal to maintain DDD boundaries
    private readonly List<ProjectMember> _members = [];
    internal IReadOnlyCollection<ProjectMember> ProjectMembers => _members.AsReadOnly();

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
        string initialApiKeyHash,
        Guid? id = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(initialApiKeyHash))
            throw new ArgumentException("An initial API key hash is required.", nameof(initialApiKeyHash));

        Id = id ?? Guid.NewGuid();
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
    /// Rotates the API key. The newly provided key becomes the primary immediately.
    /// The old primary key is demoted to a secondary key and remains valid as a fallback 
    /// until the specified expiration date.
    /// </summary>
    public void RotateApiKey(string newKeyHash, DateTimeOffset? fallbackExpiration)
    {
        if (string.IsNullOrWhiteSpace(newKeyHash))
            throw new ArgumentException("New key hash is required.", nameof(newKeyHash));

        if (fallbackExpiration.HasValue && fallbackExpiration <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Fallback expiration must be in the future.", nameof(fallbackExpiration));

        // 1. Demote the current primary key to the secondary slot with a ticking clock
        SecondaryApiKeyHash = ApiKeyHash;
        SecondaryApiKeyExpiresAt = fallbackExpiration;

        // 2. Set the newly generated key as the absolute primary
        ApiKeyHash = newKeyHash;

        // Fire the event!
        _domainEvents.Add(new ProjectApiKeyRotatedEvent(Id, OrganizationId));
    }

    /// <summary>
    /// Forcibly sets a new primary API key and immediately revokes any secondary keys.
    /// Use this ONLY when a key is compromised and you need to cut off all old access instantly.
    /// </summary>
    public void ForceSetPrimaryApiKey(string newKeyHash)
    {
        if (string.IsNullOrWhiteSpace(newKeyHash))
            throw new ArgumentException("New key hash is required.", nameof(newKeyHash));

        ApiKeyHash = newKeyHash;

        // Nuking the secondary key ensures total lockdown
        RevokeSecondaryApiKey();

        _domainEvents.Add(new ProjectApiKeyRotatedEvent(Id, OrganizationId));
    }

    /// <summary>
    /// Manually revokes the secondary (fallback) API key before its expiration date.
    /// Used if a client finishes updating their systems early and wants to close the security window.
    /// </summary>
    public void RevokeSecondaryApiKey()
    {
        SecondaryApiKeyHash = null;
        SecondaryApiKeyExpiresAt = null;
    }

    // ──────────────────────────────────────────────────────────────
    // Team membership
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the team assignment, handling new assignments, reassignments, and unassignments.
    /// </summary>
    public void ChangeTeam(Guid? targetTeamId)
    {
        // If nothing changed, do nothing!
        if (TeamId == targetTeamId) return;

        // Reject explicitly invalid data
        if (targetTeamId.HasValue && targetTeamId.Value == Guid.Empty)
            throw new ArgumentException("Team ID cannot be an empty GUID.", nameof(targetTeamId));

        // Handle the assignment / reassignment
        if (targetTeamId.HasValue)
        {
            TeamId = targetTeamId.Value;
            // Fire the assignment event!
            _domainEvents.Add(new ProjectTeamAssignedEvent(Id, TeamId.Value));
        }
        // 4. Handle the unassignment
        else
        {
            TeamId = null;
            // Optional: Fire an unassignment event if needed!
            _domainEvents.Add(new ProjectTeamUnassignedEvent(Id));
        }
    }

    public void RemoveFromTeam() => TeamId = null;

    // ──────────────────────────────────────────────────────────────
    // Basic mutations
    // ──────────────────────────────────────────────────────────────

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Project name is required.", nameof(newName));
        Name = newName.Trim();
    }
}
