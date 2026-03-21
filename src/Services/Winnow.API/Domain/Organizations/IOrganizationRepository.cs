using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Organizations;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetWithProjectsAsync(Guid id, CancellationToken cancellationToken = default);
}
