using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Organizations;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetWithProjectsAsync(Guid id, CancellationToken cancellationToken = default);
}
