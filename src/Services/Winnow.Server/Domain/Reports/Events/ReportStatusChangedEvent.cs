using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Reports.ValueObjects;

namespace Winnow.Server.Domain.Reports.Events;

public sealed record ReportStatusChangedEvent(Guid ReportId, ReportStatus OldStatus, ReportStatus NewStatus) : IDomainEvent;
