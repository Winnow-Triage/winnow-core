using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationCreatedEvent(Guid OrganizationId, string Name, string Email) : IDomainEvent;
