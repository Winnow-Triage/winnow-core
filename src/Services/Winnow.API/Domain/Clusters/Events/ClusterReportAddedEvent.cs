using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Clusters.Events;

public sealed record ClusterReportAddedEvent(Guid ClusterId, Guid ProjectId, Guid OrganizationId, Guid ReportId) : IDomainEvent;
