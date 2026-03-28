using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationMemberUnlockedEvent(Guid MemberId, Guid OrganizationId) : IDomainEvent;
