using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Projects;

namespace Winnow.API.Infrastructure.Persistence.Repositories;

public class EfProjectRepository(WinnowDbContext dbContext)
    : EfRepository<Project>(dbContext), IProjectRepository
{
    public async Task<Project?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Projects
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
