using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Clusters.Events;

public sealed record ClusterSummarizedEvent(
    Guid ClusterId,
    Guid ProjectId,
    Guid OrganizationId,
    int CriticalityScore,
    string Title,
    string Summary) : IDomainEvent;

