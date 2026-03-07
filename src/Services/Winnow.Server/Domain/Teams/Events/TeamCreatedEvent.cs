using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Teams.Events;

public sealed record TeamCreatedEvent(Guid TeamId, Guid OrganizationId, string Name) : IDomainEvent;
