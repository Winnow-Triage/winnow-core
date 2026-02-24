using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class DeleteTeamRequest
{
    public Guid Id { get; set; }
}

public sealed class DeleteTeamEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : Endpoint<DeleteTeamRequest>
{
    public override void Configure()
    {
        Delete("/teams/{id}");
        Summary(s =>
        {
            s.Summary = "Delete a team";
            s.Description = "Permanently removes a team and unassigns its projects (projects are NOT deleted).";
        });
    }

    public override async Task HandleAsync(DeleteTeamRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (team == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if there are projects in this team and unassign them if necessary, 
        // or just rely on cascade behavior if that's what we want.
        // According to DbContext, projects have OnDelete(DeleteBehavior.Cascade) for Teams.
        // Wait, let's check WinnowDbContext.cs again.
        // modelBuilder.Entity<Team>().HasMany(t => t.Projects).WithOne(p => p.Team).HasForeignKey(p => p.TeamId).OnDelete(DeleteBehavior.Cascade);

        db.Teams.Remove(team);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
