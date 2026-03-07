namespace Winnow.Server.Domain.Events;

public record IntegrationConfigUpdatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
