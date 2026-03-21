using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationRenamedEvent(Guid OrganizationId, string Name) : IDomainEvent;
