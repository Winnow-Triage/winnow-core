using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class DeleteTeamRequest : OrganizationScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class DeleteTeamEndpoint(WinnowDbContext db) : OrganizationScopedEndpoint<DeleteTeamRequest>
{
    public override void Configure()
    {
        Delete("/teams/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a team";
            s.Description = "Permanently removes a team and unassigns its projects (projects are NOT deleted).";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DeleteTeamRequest req, CancellationToken ct)
    {

        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.OrganizationId == req.CurrentOrganizationId, ct);

        if (team == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Unassign projects (set TeamId to null)
        await db.Projects
            .Where(p => p.TeamId == team.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TeamId, (Guid?)null), ct);

        // Remove team members
        await db.TeamMembers
            .Where(tm => tm.TeamId == team.Id)
            .ExecuteDeleteAsync(ct);

        db.Teams.Remove(team);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
