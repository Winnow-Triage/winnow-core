using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class AddTeamMemberRequest
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class AddTeamMemberEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : Endpoint<AddTeamMemberRequest>
{
    public override void Configure()
    {
        Post("/teams/{teamId}/members");
        Summary(s =>
        {
            s.Summary = "Add a member to a team";
            s.Description = "Assigns an organization member to a specific team.";
        });
    }

    public override async Task HandleAsync(AddTeamMemberRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        // Verify team belongs to current organization
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == req.TeamId && t.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);
        if (team == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Verify user is a member of the organization
        var isOrgMember = await db.OrganizationMembers.AnyAsync(om => om.OrganizationId == tenantContext.CurrentOrganizationId.Value && om.UserId == req.UserId, ct);
        if (!isOrgMember)
        {
            ThrowError("User is not a member of this organization.");
        }

        // Check if already a member of the team
        var alreadyMember = await db.TeamMembers.AnyAsync(tm => tm.TeamId == req.TeamId && tm.UserId == req.UserId, ct);
        if (alreadyMember)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var member = new TeamMember
        {
            TeamId = req.TeamId,
            UserId = req.UserId
        };

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
