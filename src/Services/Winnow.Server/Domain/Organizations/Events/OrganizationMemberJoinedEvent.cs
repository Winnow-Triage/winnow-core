using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationMemberJoinedEvent(Guid MemberId, Guid OrganizationId, string UserId, string Role) : IDomainEvent;
