using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations.Events;

public sealed record OrganizationMemberRoleChangedEvent(Guid MemberId, Guid OrganizationId, string OldRole, string NewRole) : IDomainEvent;
