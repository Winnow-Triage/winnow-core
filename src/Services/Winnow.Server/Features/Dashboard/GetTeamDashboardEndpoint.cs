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

        if (!Guid.TryParse(userId, out var userGuid))
        {
            ThrowError("Invalid user ID format", 401);
        }

        // 💥 THE FIX: Query the OrganizationMembers DbSet directly!
        var userHasAccess = await dbContext.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == req.TeamId &&
                           t.OrganizationId == organizationId &&
                           dbContext.OrganizationMembers.Any(om => om.OrganizationId == organizationId && om.UserId == userGuid.ToString()), ct);

        if (!userHasAccess)
        {
            ThrowError("Team not found or access denied", 404);
        }

        var metrics = await dashboardService.GetTeamDashboardAsync(organizationId, req.TeamId, ct);
        await Send.OkAsync(metrics, ct);
    }
}