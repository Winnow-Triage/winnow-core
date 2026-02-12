namespace Winnow.Server.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "New";
    public Guid? ParentTicketId { get; set; }
    public string? AssignedTo { get; set; }
    public string? Summary { get; set; } // AI-generated summary of the cluster
    public float? ConfidenceScore { get; set; } // 0.0 - 1.0 (1.0 = High Confidence, 0.0 = Low)
    public int? CriticalityScore { get; set; } // 1-10
    public string? CriticalityReasoning { get; set; }
    public byte[]? Embedding { get; set; }
}
