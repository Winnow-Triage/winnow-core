namespace Winnow.Server.Domain.Events;

public record ReportAddedToClusterEvent(Guid ClusterId, Guid ReportId) : IDomainEvent;
