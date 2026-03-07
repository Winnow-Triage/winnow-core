namespace Winnow.Server.Domain.Events;

public record AssetScanVirusDetectedEvent(Guid AssetId, Guid OrganizationId) : IDomainEvent;
