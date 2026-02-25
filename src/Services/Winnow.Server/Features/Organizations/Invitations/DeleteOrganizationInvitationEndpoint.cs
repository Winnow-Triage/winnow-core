using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Invitations;

public sealed class DeleteOrganizationInvitationEndpoint(WinnowDbContext db)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/organizations/{orgId:guid}/invitations/{invitationId:guid}");
        Summary(s =>
        {
            s.Summary = "Delete a pending invitation";
            s.Description = "Deletes an invitation that has not been accepted yet.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgId = Route<Guid>("orgId");
        var invitationId = Route<Guid>("invitationId");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == orgId && om.UserId == userId && (om.Role == "owner" || om.Role == "Admin"), ct);

        if (!isOwner)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Id == invitationId && oi.OrganizationId == orgId, ct);

        if (invitation == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        db.OrganizationInvitations.Remove(invitation);
        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
