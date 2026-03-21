using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, Guid OrganizationId, string OwnerId) : IDomainEvent;
