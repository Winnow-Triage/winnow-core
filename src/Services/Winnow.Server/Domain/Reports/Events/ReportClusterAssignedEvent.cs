using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Common;

namespace Winnow.Server.Domain.Reports.Events;

public sealed record ReportClusterAssignedEvent(Guid ReportId, Guid ClusterId, ConfidenceScore? ConfidenceScore) : IDomainEvent;
