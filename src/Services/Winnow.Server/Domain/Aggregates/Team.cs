using Winnow.Server.Domain.Events;

namespace Winnow.Server.Domain.Aggregates;

/// <summary>
/// Represents a team within an organization. Teams group projects together
/// and allow role-based access control at the team level.
/// </summary>
public class Team : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Private EF constructor
    private Team() => Name = null!;

    public Team(Guid organizationId, string name)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name is required.", nameof(name));

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name;
        CreatedAt = DateTime.UtcNow;

        _domainEvents.Add(new TeamCreatedEvent(Id, OrganizationId, Name));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Team name is required.", nameof(newName));
        if (newName == Name) return;

        var old = Name;
        Name = newName;
        _domainEvents.Add(new TeamRenamedEvent(Id, old, newName));
    }
}
