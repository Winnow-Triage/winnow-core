using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects.Events;

public sealed record ProjectApiKeyRotatedEvent(Guid ProjectId, Guid OrganizationId) : IDomainEvent;
