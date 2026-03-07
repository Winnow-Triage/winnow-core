namespace Winnow.Server.Domain.Events;

public record ProjectTeamAssignedEvent(Guid ProjectId, Guid TeamId) : IDomainEvent;
