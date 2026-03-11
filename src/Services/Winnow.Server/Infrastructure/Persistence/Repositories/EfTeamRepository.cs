using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Teams;

namespace Winnow.Server.Infrastructure.Persistence.Repositories;

public class EfTeamRepository(WinnowDbContext dbContext)
    : EfRepository<Team>(dbContext), ITeamRepository
{
    public async Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Teams
            .Include(t => t.TeamMembers)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Team>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Teams
            .Where(t => t.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }
}
