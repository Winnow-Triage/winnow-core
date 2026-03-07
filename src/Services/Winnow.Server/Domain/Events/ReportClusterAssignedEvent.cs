namespace Winnow.Server.Domain.Events;

public record ReportClusterAssignedEvent(Guid ReportId, Guid ClusterId) : IDomainEvent;
