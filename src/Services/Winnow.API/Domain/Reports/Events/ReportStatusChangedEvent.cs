using Winnow.API.Domain.Core;
using Winnow.API.Domain.Reports.ValueObjects;

namespace Winnow.API.Domain.Reports.Events;

public sealed record ReportStatusChangedEvent(Guid ReportId, ReportStatus OldStatus, ReportStatus NewStatus) : IDomainEvent;
