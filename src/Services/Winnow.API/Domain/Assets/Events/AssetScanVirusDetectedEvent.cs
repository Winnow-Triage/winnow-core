using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Assets.Events;

public sealed record AssetScanVirusDetectedEvent(Guid AssetId, Guid OrganizationId) : IDomainEvent;
