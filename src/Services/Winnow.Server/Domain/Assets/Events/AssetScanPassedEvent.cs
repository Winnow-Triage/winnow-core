using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Assets.Events;

public sealed record AssetScanPassedEvent(Guid AssetId, Guid OrganizationId, string CleanS3Key) : IDomainEvent;
