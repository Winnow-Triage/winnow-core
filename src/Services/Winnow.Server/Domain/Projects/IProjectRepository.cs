using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Projects;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
