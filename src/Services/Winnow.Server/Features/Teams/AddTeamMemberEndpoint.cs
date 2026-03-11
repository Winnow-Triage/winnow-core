using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class AddTeamMemberRequest : OrganizationScopedRequest
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class AddTeamMemberEndpoint(WinnowDbContext db)
    : OrganizationScopedEndpoint<AddTeamMemberRequest>
{
    public override void Configure()
    {
        Post("/teams/{teamId}/members");
        Summary(s =>
        {
            s.Summary = "Add a member to a team";
            s.Description = "Assigns an organization member to a specific team.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AddTeamMemberRequest req, CancellationToken ct)
    {
        // Check if already a member of the team
        var alreadyMember = await db.TeamMembers.AnyAsync(tm => tm.TeamId == req.TeamId && tm.UserId == req.UserId, ct);
        if (alreadyMember)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var member = new Winnow.Server.Domain.Teams.TeamMember(req.TeamId, req.UserId);

        db.TeamMembers.Add(member);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
