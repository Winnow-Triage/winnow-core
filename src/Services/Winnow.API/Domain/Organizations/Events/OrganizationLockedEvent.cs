using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationLockedEvent(Guid OrganizationId) : IDomainEvent;