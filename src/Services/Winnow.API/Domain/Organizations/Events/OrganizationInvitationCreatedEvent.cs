using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationInvitationCreatedEvent(Guid InvitationId, Guid OrganizationId, string Email, string Token) : IDomainEvent;
