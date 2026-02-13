using Winnow.Server.Entities;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public class PlaceholderSummaryService : IClusterSummaryService
{
    public async Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Ticket> tickets, CancellationToken ct)
    {
        // Simulate LLM latency
        await Task.Delay(1000, ct);

        var count = tickets.Count();
        if (count == 0)
        {
            return new ClusterSummaryResult("🤖 (Placeholder) No tickets provided for summary generation.", null, null);
        }

        var titles = tickets.Take(3).Select(t => t.Title);
        var joinedTitles = string.Join(", ", titles);
        var suffix = count > 3 ? $" and {count - 3} more..." : "";

        var summary = $"🤖 (Placeholder) This cluster contains {count} tickets, including: {joinedTitles}{suffix}. \n\nIt appears to be related to a recurring issue. \n\nRecommended action: Investigate the root cause in the logs.";
        return new ClusterSummaryResult(summary, 5, "Placeholder reasoning: Randomly assigned medium criticality.");
    }
}
