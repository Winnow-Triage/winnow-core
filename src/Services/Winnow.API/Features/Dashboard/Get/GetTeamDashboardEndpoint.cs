using Winnow.API.Features.Dashboard.Dtos;
using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Dashboard.Get;

public class GetTeamDashboardRequest
{
    public Guid TeamId { get; set; }
}

public sealed class GetTeamDashboardEndpoint(IMediator mediator) : Endpoint<GetTeamDashboardRequest, TeamDashboardDto>
{
    public override void Configure()
    {
        Get("/dashboard/teams/{TeamId}/metrics");
        Summary(s =>
        {
            s.Summary = "Get team dashboard metrics";
            s.Description = "Retrieves aggregated metrics for all projects in a specific team.";
            s.Response<TeamDashboardDto>(200, "Metrics data");
            s.Response(401, "Unauthorized");
            s.Response(404, "Team not found or access denied");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetTeamDashboardRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdClaim = User.FindFirstValue("organization");

        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
            return;
        }

        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            ThrowError("Organization ID not found in token", 401);
            return;
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            ThrowError("Invalid user ID format", 401);
            return;
        }

        var query = new GetTeamDashboardQuery(organizationId, req.TeamId, userId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                ThrowError(result.ErrorMessage ?? "Team not found or access denied", 404);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}