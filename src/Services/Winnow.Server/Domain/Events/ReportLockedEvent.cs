namespace Winnow.Server.Domain.Events;

public record ReportLockedEvent(Guid ReportId, Guid OrganizationId) : IDomainEvent;
