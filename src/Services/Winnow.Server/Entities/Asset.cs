namespace Winnow.Server.Entities;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Entity Framework requires public entities")]
public enum AssetStatus
{
    Pending,
    Clean,
    Infected,
    Failed
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Entity Framework requires public entities")]
public class Asset : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Keys
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ReportId { get; set; }

    // File metadata
    public string FileName { get; set; } = default!;
    public string S3Key { get; set; } = default!;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }

    // Scan tracking
    public AssetStatus Status { get; set; } = AssetStatus.Pending;
    public string? CleanS3Key { get; set; } // Set by Bouncer after scan passes

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScannedAt { get; set; }

    // Navigation
    public Report Report { get; set; } = null!;
}
