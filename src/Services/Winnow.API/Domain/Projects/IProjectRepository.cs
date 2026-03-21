using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Projects;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
