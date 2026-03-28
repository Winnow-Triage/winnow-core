using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects.Events;

public sealed record ProjectTeamAssignedEvent(Guid ProjectId, Guid TeamId) : IDomainEvent;
