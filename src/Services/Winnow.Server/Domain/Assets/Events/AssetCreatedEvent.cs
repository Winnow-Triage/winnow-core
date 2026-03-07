using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Assets.Events;

public sealed record AssetCreatedEvent(Guid AssetId, Guid OrganizationId, Guid ReportId) : IDomainEvent;
