using Winnow.API.Domain.Ai;
using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Reports;

namespace Winnow.API.Services.Ai;

public class PlaceholderClusterSummaryService : IClusterSummaryService
{
    public async Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct)
    {
        // Simulate LLM latency
        await Task.Delay(1000, ct);

        var count = reports.Count();
        if (count == 0)
        {
            return new ClusterSummaryResult("Empty Cluster", "🤖 (Placeholder) No reports provided for summary generation.", null, null, null);
        }

        var messages = reports.Take(3).Select(t => t.Message);
        var joinedMessages = string.Join(", ", messages);
        var suffix = count > 3 ? $" and {count - 3} more..." : "";

        var summary = $"🤖 (Placeholder) This cluster contains {count} reports, including: {joinedMessages}{suffix}. \n\nIt appears to be related to a recurring issue. \n\nRecommended action: Investigate the root cause in the logs.";
        var usage = new AiUsageInfo(50, 150, "placeholder-model", "Placeholder");
        return new ClusterSummaryResult("🤖 (Placeholder) Recurring Issue", summary, 5, "Placeholder reasoning: Randomly assigned medium criticality.", Usage: usage);
    }
}
