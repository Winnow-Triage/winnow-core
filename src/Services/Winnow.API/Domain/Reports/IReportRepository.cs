using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Reports;

public interface IReportRepository : IRepository<Report>
{
    Task<IReadOnlyList<Report>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Report>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
