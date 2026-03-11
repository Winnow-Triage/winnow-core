using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations.Events;

public sealed record OrganizationInvitationCreatedEvent(Guid InvitationId, Guid OrganizationId, string Email, string Token) : IDomainEvent;
