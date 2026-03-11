using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationMemberLockedEvent(Guid MemberId, Guid OrganizationId) : IDomainEvent;
