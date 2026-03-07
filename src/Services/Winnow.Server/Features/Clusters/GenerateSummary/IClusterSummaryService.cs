using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Reports;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

public record ClusterSummaryResult(string Title, string Summary, int? CriticalityScore, string? CriticalityReasoning, bool IsError = false);

public interface IClusterSummaryService
{
    Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct);
}
