using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationLockedEvent(Guid OrganizationId) : IDomainEvent;