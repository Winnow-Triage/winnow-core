using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationInvitationRevokedEvent(Guid InvitationId, Guid OrganizationId) : IDomainEvent;
