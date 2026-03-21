using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Assets.Events;

public sealed record AssetScanPassedEvent(Guid AssetId, Guid OrganizationId, string CleanS3Key) : IDomainEvent;
