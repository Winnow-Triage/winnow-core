using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Teams;

public interface ITeamRepository : IRepository<Team>
{
    Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
