using Winnow.API.Domain.Core;
using Winnow.API.Domain.Common;

namespace Winnow.API.Domain.Reports.Events;

public sealed record ReportClusterAssignedEvent(Guid ReportId, Guid ClusterId, ConfidenceScore? ConfidenceScore) : IDomainEvent;
