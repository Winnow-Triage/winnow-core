using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Reports.Events;

public sealed record ReportLockedEvent(Guid ReportId, Guid OrganizationId) : IDomainEvent;
