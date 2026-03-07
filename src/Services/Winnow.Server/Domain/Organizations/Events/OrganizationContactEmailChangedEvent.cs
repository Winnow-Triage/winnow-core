using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationContactEmailChangedEvent(Guid OrganizationId, string Email) : IDomainEvent;
