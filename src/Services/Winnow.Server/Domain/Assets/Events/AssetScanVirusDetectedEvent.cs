using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Assets.Events;

public sealed record AssetScanVirusDetectedEvent(Guid AssetId, Guid OrganizationId) : IDomainEvent;
