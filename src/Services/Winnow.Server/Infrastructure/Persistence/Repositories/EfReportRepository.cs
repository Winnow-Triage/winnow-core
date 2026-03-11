using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports;

namespace Winnow.Server.Infrastructure.Persistence.Repositories;

public class EfReportRepository(WinnowDbContext dbContext)
    : EfRepository<Report>(dbContext), IReportRepository
{
    public async Task<IReadOnlyList<Report>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Reports
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Report>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await DbContext.Reports
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
