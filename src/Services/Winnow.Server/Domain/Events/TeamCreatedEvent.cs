namespace Winnow.Server.Domain.Events;

public record TeamCreatedEvent(Guid TeamId, Guid OrganizationId, string Name) : IDomainEvent;
