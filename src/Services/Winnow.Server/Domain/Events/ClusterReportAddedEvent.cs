namespace Winnow.Server.Domain.Events;

public record ClusterReportAddedEvent(Guid ClusterId, Guid ReportId) : IDomainEvent;
