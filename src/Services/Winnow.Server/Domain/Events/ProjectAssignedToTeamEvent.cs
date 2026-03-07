namespace Winnow.Server.Domain.Events;

public record ProjectAssignedToTeamEvent(Guid ProjectId, Guid TeamId) : IDomainEvent;
