using Winnow.Server.Entities;

namespace Winnow.Server.Features.Reports.GenerateSummary;

public record ClusterSummaryResult(string Title, string Summary, int? CriticalityScore, string? CriticalityReasoning, bool IsError = false);

public interface IClusterSummaryService
{
    Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Report> reports, CancellationToken ct);
}
