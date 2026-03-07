using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Clusters.Events;

public sealed record ClusterMergeSuggestedEvent(Guid ClusterId, Guid TargetClusterId, double ConfidenceScore) : IDomainEvent;
