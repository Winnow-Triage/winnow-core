using Winnow.Server.Entities;

namespace Winnow.Server.Features.Reports.GenerateSummary;

public record ClusterSummaryResult(string Summary, int? CriticalityScore, string? CriticalityReasoning);

public interface IClusterSummaryService
{
    Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct);
}
