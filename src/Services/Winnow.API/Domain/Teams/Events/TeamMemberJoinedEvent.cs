using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Teams.Events;

public sealed record TeamMemberJoinedEvent(Guid MemberId, Guid TeamId, string UserId) : IDomainEvent;
