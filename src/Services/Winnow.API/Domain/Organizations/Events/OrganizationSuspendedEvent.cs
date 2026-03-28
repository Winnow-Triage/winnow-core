using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationSuspendedEvent(Guid OrganizationId, string Reasoning) : IDomainEvent;
