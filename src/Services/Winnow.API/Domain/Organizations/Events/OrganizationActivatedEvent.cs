using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationActivatedEvent(Guid OrganizationId) : IDomainEvent;
