using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationRenamedEvent(Guid OrganizationId, string Name) : IDomainEvent;
