using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Dashboard.Get;

[RequirePermission("reports:read")]
public class GetOrganizationDashboardQuery(Guid currentOrganizationId) : IRequest<GetOrganizationDashboardResult>, IOrgScopedRequest
{
    public Guid CurrentOrganizationId { get; set; } = currentOrganizationId;
}

public record GetOrganizationDashboardResult(bool IsSuccess, OrganizationDashboardDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetOrganizationDashboardHandler(IDashboardService dashboardService) : IRequestHandler<GetOrganizationDashboardQuery, GetOrganizationDashboardResult>
{
    public async Task<GetOrganizationDashboardResult> Handle(GetOrganizationDashboardQuery request, CancellationToken cancellationToken)
    {
        var metrics = await dashboardService.GetOrganizationDashboardAsync(request.CurrentOrganizationId, cancellationToken);

        return new GetOrganizationDashboardResult(true, metrics);
    }
}
