using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Reports;

public interface IReportRepository : IRepository<Report>
{
    Task<IReadOnlyList<Report>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Report>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
