using Winnow.Server.Entities;

namespace Winnow.Server.Features.Tickets.GenerateSummary;

public interface IClusterSummaryService
{
    Task<string> GenerateSummaryAsync(IEnumerable<Ticket> tickets, CancellationToken ct);
}
