namespace Winnow.Server.Domain.Events;

public record AssetScanFailedEvent(Guid AssetId, Guid OrganizationId, string ErrorMessage) : IDomainEvent;
