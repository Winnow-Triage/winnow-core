using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;

namespace Winnow.Server.Features.Dashboard.Get;

public record GetOrganizationDashboardQuery(Guid OrganizationId) : IRequest<GetOrganizationDashboardResult>;

public record GetOrganizationDashboardResult(bool IsSuccess, OrganizationDashboardDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetOrganizationDashboardHandler(IDashboardService dashboardService) : IRequestHandler<GetOrganizationDashboardQuery, GetOrganizationDashboardResult>
{
    public async Task<GetOrganizationDashboardResult> Handle(GetOrganizationDashboardQuery request, CancellationToken cancellationToken)
    {
        var metrics = await dashboardService.GetOrganizationDashboardAsync(request.OrganizationId, cancellationToken);

        return new GetOrganizationDashboardResult(true, metrics);
    }
}
