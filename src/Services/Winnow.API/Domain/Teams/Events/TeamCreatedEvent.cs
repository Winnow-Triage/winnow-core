using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Teams.Events;

public sealed record TeamCreatedEvent(Guid TeamId, Guid OrganizationId, string Name) : IDomainEvent;
