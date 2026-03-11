using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Teams.Events;

public sealed record TeamMemberJoinedEvent(Guid MemberId, Guid TeamId, string UserId) : IDomainEvent;
