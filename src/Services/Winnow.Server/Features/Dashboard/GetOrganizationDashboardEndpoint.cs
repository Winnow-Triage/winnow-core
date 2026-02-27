using System.Security.Claims;
using FastEndpoints;

namespace Winnow.Server.Features.Dashboard;

public sealed class GetOrganizationDashboardEndpoint(IDashboardService dashboardService) : EndpointWithoutRequest<OrganizationDashboardDto>
{
    public override void Configure()
    {
        Get("/dashboard/organization/metrics");
        Summary(s =>
        {
            s.Summary = "Get organization dashboard metrics";
            s.Description = "Retrieves aggregated metrics across all projects for the organization.";
            s.Response<OrganizationDashboardDto>(200, "Metrics data");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var organizationIdClaim = User.FindFirstValue("organization");

        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            ThrowError("Organization ID not found in token", 401);
        }

        var metrics = await dashboardService.GetOrganizationDashboardAsync(organizationId, ct);
        await Send.OkAsync(metrics, ct);
    }
}
