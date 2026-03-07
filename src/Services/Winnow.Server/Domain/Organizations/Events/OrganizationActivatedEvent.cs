using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationActivatedEvent(Guid OrganizationId) : IDomainEvent;
