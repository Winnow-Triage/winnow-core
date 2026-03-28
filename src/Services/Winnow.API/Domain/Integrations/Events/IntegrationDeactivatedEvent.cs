using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Integrations.Events;

public sealed record IntegrationDeactivatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
