using Winnow.Server.Features.Dashboard.Dtos;
using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Dashboard.Get;

public sealed class GetOrganizationDashboardEndpoint(IMediator mediator) : EndpointWithoutRequest<OrganizationDashboardDto>
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
            return;
        }

        var query = new GetOrganizationDashboardQuery(organizationId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
