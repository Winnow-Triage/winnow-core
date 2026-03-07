namespace Winnow.Server.Domain.Events;

public record IntegrationReactivatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
