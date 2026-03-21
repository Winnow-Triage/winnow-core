using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects.Events;

public sealed record ProjectMemberJoinedEvent(Guid MemberId, Guid ProjectId, string UserId) : IDomainEvent;
