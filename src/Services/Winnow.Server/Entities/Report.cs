namespace Winnow.Server.Entities;

public class Report : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; } // Foreign Key
    public Guid OrganizationId { get; set; } // Tenant isolation
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "New";
    public Guid? ClusterId { get; set; }
    public Guid? SuggestedClusterId { get; set; }
    public float? SuggestedConfidenceScore { get; set; }

    // Billing/Quota fields
    public bool IsOverage { get; set; }
    public bool IsLocked { get; set; }

    public string? AssignedTo { get; set; }
    public float? ConfidenceScore { get; set; }
    public float[]? Embedding { get; set; }
    public string? StackTraceHash { get; set; }
    public Uri? ExternalUrl { get; set; }
    public string? Metadata { get; set; }
    public string? Screenshot { get; set; } // S3 object key (legacy, replaced by Assets)

    // Navigation
    public Project? Project { get; set; }
    public Cluster? Cluster { get; set; }
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
