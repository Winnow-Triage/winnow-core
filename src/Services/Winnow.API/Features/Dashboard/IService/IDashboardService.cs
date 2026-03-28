using Winnow.API.Features.Dashboard.Dtos;
namespace Winnow.API.Features.Dashboard.IService;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid organizationId, Guid? projectId = null, Guid? teamId = null, CancellationToken ct = default);
    Task<OrganizationDashboardDto> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken ct = default);
    Task<TeamDashboardDto> GetTeamDashboardAsync(Guid organizationId, Guid teamId, CancellationToken ct = default);
}
