using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, Guid OrganizationId, string OwnerId) : IDomainEvent;
