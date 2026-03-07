namespace Winnow.Server.Domain.Events;

public record ClusterMergeSuggestedEvent(Guid ClusterId, Guid TargetClusterId, double ConfidenceScore) : IDomainEvent;
