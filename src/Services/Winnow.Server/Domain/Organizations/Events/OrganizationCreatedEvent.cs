using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationCreatedEvent(Guid OrganizationId, string Name, string Email) : IDomainEvent;
