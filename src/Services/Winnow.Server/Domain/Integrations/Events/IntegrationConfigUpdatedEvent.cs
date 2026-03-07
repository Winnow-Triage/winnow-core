using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Integrations.Events;

public sealed record IntegrationConfigUpdatedEvent(Guid IntegrationId, Guid ProjectId, string Provider) : IDomainEvent;
