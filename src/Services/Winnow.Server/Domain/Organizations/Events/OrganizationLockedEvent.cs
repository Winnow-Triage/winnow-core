using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public record OrganizationLockedEvent(Guid OrganizationId) : IDomainEvent;