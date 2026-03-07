using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Integrations.Events;

public sealed record IntegrationReactivatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
