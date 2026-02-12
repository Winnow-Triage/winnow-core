using Winnow.Server.Entities;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public record ClusterSummaryResult(string Summary, int? CriticalityScore, string? CriticalityReasoning);

public interface IClusterSummaryService
{
    Task<ClusterSummaryResult> GenerateSummaryAsync(IEnumerable<Ticket> tickets, CancellationToken ct);
}
