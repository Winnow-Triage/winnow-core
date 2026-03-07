using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Teams;

public class RemoveTeamMemberRequest : OrganizationScopedRequest
{
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class RemoveTeamMemberEndpoint(WinnowDbContext db)
    : OrganizationScopedEndpoint<RemoveTeamMemberRequest>
{
    public override void Configure()
    {
        Delete("/teams/{teamId}/members/{userId}");
        Summary(s =>
        {
            s.Summary = "Remove a member from a team";
            s.Description = "Dissociates a user from a specific team.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RemoveTeamMemberRequest req, CancellationToken ct)
    {
        var member = await db.TeamMembers
            .Include(tm => tm.Team)
            .FirstOrDefaultAsync(tm =>
                tm.TeamId == req.TeamId &&
                tm.UserId == req.UserId &&
                tm.Team!.OrganizationId == req.CurrentOrganizationId, ct);

        if (member == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        db.TeamMembers.Remove(member);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
