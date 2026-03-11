using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects.Events;

public sealed record ProjectMemberJoinedEvent(Guid MemberId, Guid ProjectId, string UserId) : IDomainEvent;
