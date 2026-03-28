using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Assets.Events;

public sealed record AssetScanFailedEvent(Guid AssetId, Guid OrganizationId, string ErrorMessage) : IDomainEvent;
