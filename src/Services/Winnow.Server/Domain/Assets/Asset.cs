using Winnow.Server.Domain.Assets.Events;
using Winnow.Server.Domain.Assets.ValueObjects;
using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Assets;

/// <summary>
/// Represents a file uploaded to Winnow, such as a screenshot, video, or log file attached to a Report.
/// Assets go through an antivirus scanning lifecycle before being served back to users.
/// </summary>
public class Asset : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ReportId { get; private set; }

    public string FileName { get; private set; }
    public string S3Key { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    public AssetStatus Status { get; private set; }
    public string? CleanS3Key { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ScannedAt { get; private set; }

    public void UpdateContentType(string contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            ContentType = contentType;
        }
    }

    // Private EF constructor
    private Asset()
    {
        FileName = null!;
        S3Key = null!;
        ContentType = null!;
    }

    public Asset(
        Guid organizationId,
        Guid projectId,
        Guid reportId,
        string fileName,
        string s3Key,
        long sizeBytes,
        string contentType = "application/octet-stream")
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report ID is required.", nameof(reportId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(s3Key))
            throw new ArgumentException("S3 key is required.", nameof(s3Key));
        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size bytes must be strictly positive.");

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ProjectId = projectId;
        ReportId = reportId;
        FileName = fileName;
        S3Key = s3Key;
        SizeBytes = sizeBytes;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        Status = AssetStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        _domainEvents.Add(new AssetCreatedEvent(Id, organizationId, reportId));

    }

    // ──────────────────────────────────────────────────────────────
    // Scanning Lifecycle
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the antivirus scanner confirms the file is safe and has moved it to the clean bucket.
    /// </summary>
    public void MarkAsClean(string cleanS3Key)
    {
        if (string.IsNullOrWhiteSpace(cleanS3Key))
            throw new ArgumentException("Clean S3 key is required.", nameof(cleanS3Key));

        if (Status != AssetStatus.Pending && Status != AssetStatus.Failed)
            throw new InvalidOperationException($"Cannot transition asset to Clean from {Status}.");

        Status = AssetStatus.Clean;
        CleanS3Key = cleanS3Key;
        ScannedAt = DateTime.UtcNow;

        _domainEvents.Add(new AssetScanPassedEvent(Id, OrganizationId, cleanS3Key));
    }

    /// <summary>
    /// Called when the antivirus scanner finds malware in the file.
    /// </summary>
    public void MarkAsInfected()
    {
        if (Status != AssetStatus.Pending && Status != AssetStatus.Failed)
            throw new InvalidOperationException($"Cannot transition asset to Infected from {Status}.");

        Status = AssetStatus.Infected;
        ScannedAt = DateTime.UtcNow;

        _domainEvents.Add(new AssetScanVirusDetectedEvent(Id, OrganizationId));
    }

    /// <summary>
    /// Called when the scanning process itself fails (e.g., timeout, scanner error)
    /// </summary>
    /// <param name="errorMessage"> the message to propogate to the event </param>
    public void MarkAsFailed(string errorMessage)
    {
        if (Status != AssetStatus.Pending)
            throw new InvalidOperationException($"Cannot transition asset to Failed from {Status}.");

        Status = AssetStatus.Failed;
        ScannedAt = null; // No complete scan occurred

        _domainEvents.Add(new AssetScanFailedEvent(Id, OrganizationId, errorMessage));
    }
}
