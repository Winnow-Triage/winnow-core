namespace Winnow.Server.Domain.Events;

public record ProjectApiKeyRotatedEvent(Guid ProjectId, Guid OrganizationId) : IDomainEvent;
