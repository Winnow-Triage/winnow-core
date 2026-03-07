using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects.Events;

public sealed record ProjectTeamAssignedEvent(Guid ProjectId, Guid TeamId) : IDomainEvent;
