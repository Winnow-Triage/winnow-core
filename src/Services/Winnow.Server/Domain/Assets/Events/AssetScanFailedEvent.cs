using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Assets.Events;

public sealed record AssetScanFailedEvent(Guid AssetId, Guid OrganizationId, string ErrorMessage) : IDomainEvent;
