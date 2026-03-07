namespace Winnow.Server.Domain.Events;

public record ProjectCreatedEvent(Guid ProjectId, Guid OrganizationId, string OwnerId) : IDomainEvent;
