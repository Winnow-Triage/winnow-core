using Winnow.API.Features.Dashboard.Dtos;
using FastEndpoints;
using MediatR;
using Winnow.API.Infrastructure.MultiTenancy;

namespace Winnow.API.Features.Dashboard.Get;


public class GetDashboardMetricsRequest
{
    [FromHeader("X-Project-ID", IsRequired = true)]
    public Guid ProjectId { get; set; }
}

public sealed class GetDashboardMetricsEndpoint(
    IMediator mediator,
    ITenantContext tenantContext)
    : Endpoint<GetDashboardMetricsRequest, DashboardMetricsDto>
{
    public override void Configure()
    {
        Get("/dashboard/metrics");
        Summary(s =>
        {
            s.Summary = "Get dashboard metrics";
            s.Description = "Retrieves aggregated metrics for the project dashboard.";
            s.Response<DashboardMetricsDto>(200, "Metrics data");
            s.Response(400, "Invalid project ID");
            s.Response(404, "Project not found or access denied");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetDashboardMetricsRequest req, CancellationToken ct)
    {
        // 3. Use your TenantContext to verify auth instead of manual claim parsing
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var command = new GetDashboardMetricsQuery(orgId, req.ProjectId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}