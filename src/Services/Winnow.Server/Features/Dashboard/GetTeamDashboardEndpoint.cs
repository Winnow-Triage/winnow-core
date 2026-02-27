using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;

public class GetTeamDashboardRequest
{
    public Guid TeamId { get; set; }
}

public sealed class GetTeamDashboardEndpoint(IDashboardService dashboardService, WinnowDbContext dbContext) : Endpoint<GetTeamDashboardRequest, TeamDashboardDto>
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
        }

        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            ThrowError("Organization ID not found in token", 401);
        }

        // Validate user has access to this team in the organization
        var userHasAccess = await dbContext.Teams
            .Include(t => t.Organization!)
            .ThenInclude(o => o.Members)
            .AsNoTracking()
            .AnyAsync(t => t.Id == req.TeamId &&
                         t.OrganizationId == organizationId &&
                         t.Organization!.Members.Any(m => m.UserId == userId), ct);

        if (!userHasAccess)
        {
            ThrowError("Team not found or access denied", 404);
        }

        var metrics = await dashboardService.GetTeamDashboardAsync(organizationId, req.TeamId, ct);
        await Send.OkAsync(metrics, ct);
    }
}
