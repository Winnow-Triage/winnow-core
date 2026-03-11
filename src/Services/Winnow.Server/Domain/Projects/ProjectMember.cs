using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Projects.Events;

namespace Winnow.Server.Domain.Projects;

/// <summary>
/// Represents a membership of a user within a project.
/// </summary>
public class ProjectMember : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Private EF constructor
    private ProjectMember()
    {
        UserId = null!;
    }

    public ProjectMember(Guid projectId, string userId)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required.", nameof(userId));

        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;

        _domainEvents.Add(new ProjectMemberJoinedEvent(Id, ProjectId, UserId));
    }
}
