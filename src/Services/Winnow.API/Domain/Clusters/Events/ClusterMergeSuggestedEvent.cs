using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Clusters.Events;

public sealed record ClusterMergeSuggestedEvent(Guid ClusterId, Guid TargetClusterId, double ConfidenceScore) : IDomainEvent;
