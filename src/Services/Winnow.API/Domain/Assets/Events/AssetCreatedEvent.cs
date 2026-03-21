using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Assets.Events;

public sealed record AssetCreatedEvent(Guid AssetId, Guid OrganizationId, Guid ReportId) : IDomainEvent;
