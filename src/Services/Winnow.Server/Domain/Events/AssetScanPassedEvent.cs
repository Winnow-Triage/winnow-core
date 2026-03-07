namespace Winnow.Server.Domain.Events;

public record AssetScanPassedEvent(Guid AssetId, Guid OrganizationId, string CleanS3Key) : IDomainEvent;
