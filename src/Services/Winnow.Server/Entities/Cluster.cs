namespace Winnow.Server.Entities;

public class Cluster : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid OrganizationId { get; set; }
    public float[]? Centroid { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public int? CriticalityScore { get; set; }
    public string? CriticalityReasoning { get; set; }
    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
    public Guid? SuggestedMergeClusterId { get; set; }
    public float? SuggestedMergeConfidenceScore { get; set; }
    public DateTime? LastSummarizedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
