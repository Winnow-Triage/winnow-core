using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationSuspendedEvent(Guid OrganizationId, string Reasoning) : IDomainEvent;
