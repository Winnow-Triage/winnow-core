namespace Winnow.Server.Domain.Events;

public record IntegrationDeactivatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
