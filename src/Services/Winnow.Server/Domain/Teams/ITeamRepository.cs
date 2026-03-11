using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Teams;

public interface ITeamRepository : IRepository<Team>
{
    Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
