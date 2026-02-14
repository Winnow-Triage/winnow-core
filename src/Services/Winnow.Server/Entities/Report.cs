namespace Winnow.Server.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; } // Foreign Key
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string Status { get; set; } = "New";
    public Guid? ClusterId { get; set; } // The cluster this report belongs to
    
    // Legacy/Clustering fields (kept for compatibility during refactor)
    public Guid? ParentReportId { get; set; }
    public Guid? SuggestedParentId { get; set; }
    public float? SuggestedConfidenceScore { get; set; }
    public string? AssignedTo { get; set; }
    public string? Summary { get; set; } 
    public float? ConfidenceScore { get; set; }
    public int? CriticalityScore { get; set; }
    public string? CriticalityReasoning { get; set; }
    public byte[]? Embedding { get; set; }
    public string? StackTraceHash { get; set; }
    public string? ExternalUrl { get; set; }
    public string? Metadata { get; set; } // Renamed from MetadataJson
    public string? Screenshot { get; set; } // Base64 PNG data URL
}
