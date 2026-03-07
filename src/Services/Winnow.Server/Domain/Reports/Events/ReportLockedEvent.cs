using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Reports.Events;

public sealed record ReportLockedEvent(Guid ReportId, Guid OrganizationId) : IDomainEvent;
