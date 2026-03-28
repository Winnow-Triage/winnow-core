using Winnow.API.Domain.Core;
using Winnow.API.Domain.Teams.Events;

namespace Winnow.API.Domain.Teams;

/// <summary>
/// Represents a membership of a user within a team.
/// </summary>
public class TeamMember : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Private EF constructor
    private TeamMember()
    {
        UserId = null!;
    }

    public TeamMember(Guid teamId, string userId)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("Team ID is required.", nameof(teamId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));

        Id = Guid.NewGuid();
        TeamId = teamId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;

        _domainEvents.Add(new TeamMemberJoinedEvent(Id, TeamId, UserId));
    }
}
