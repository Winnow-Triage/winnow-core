using Winnow.API.Domain.Clusters;
using Winnow.API.Domain.Reports;

using Winnow.API.Domain.Ai;

namespace Winnow.API.Features.Clusters.GenerateSummary;

public record ClusterSummaryResult(string Title, string Summary, int? CriticalityScore, string? CriticalityReasoning, AiUsageInfo? Usage = null, bool IsError = false);

public interface IClusterSummaryService
{
    Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct);
}
