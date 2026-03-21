using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects.Events;

public sealed record ProjectApiKeyRotatedEvent(Guid ProjectId, Guid OrganizationId) : IDomainEvent;
