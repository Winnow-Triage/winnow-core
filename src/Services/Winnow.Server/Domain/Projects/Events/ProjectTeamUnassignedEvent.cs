using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects.Events;

public sealed record ProjectTeamUnassignedEvent(Guid ProjectId) : IDomainEvent;
