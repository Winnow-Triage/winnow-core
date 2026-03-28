using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Organizations;

namespace Winnow.API.Infrastructure.Persistence.Repositories;

public class EfOrganizationRepository(WinnowDbContext dbContext)
    : EfRepository<Organization>(dbContext), IOrganizationRepository
{
    public async Task<Organization?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Organizations
            .Include(o => o.OrganizationMemberships)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Organization?> GetWithProjectsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Organizations
            .Include(o => o.OrganizationProjects)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
