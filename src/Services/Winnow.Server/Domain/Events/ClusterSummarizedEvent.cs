namespace Winnow.Server.Domain.Events;

public record ClusterSummarizedEvent(Guid ClusterId, Guid OrganizationId, int CriticalityScore) : IDomainEvent;
