using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationContactEmailChangedEvent(Guid OrganizationId, string Email) : IDomainEvent;
