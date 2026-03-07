namespace Winnow.Server.Domain.Events;

public record ReportAssignedToClusterEvent(Guid ReportId, Guid ClusterId) : IDomainEvent;
