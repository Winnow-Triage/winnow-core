using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationMemberJoinedEvent(Guid MemberId, Guid OrganizationId, string UserId, string Role) : IDomainEvent;
