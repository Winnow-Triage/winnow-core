using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects.Events;

public sealed record ProjectTeamUnassignedEvent(Guid ProjectId) : IDomainEvent;
