using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Invitations;

public sealed class DeleteOrganizationInvitationEndpoint(WinnowDbContext db)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/organizations/{orgId}/invitations/{invitationId}");
        Summary(s =>
        {
            s.Summary = "Delete a pending invitation";
            s.Description = "Deletes an invitation that has not been accepted yet.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var invitationIdRaw = Route<string>("invitationId");
        Console.WriteLine($"[CANCEL] Attempting to delete invitation. OrgId (raw): {orgIdRaw}, InvId (raw): {invitationIdRaw}");

        Guid orgId = Guid.Empty;
        Guid invitationId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId) || !Guid.TryParse(invitationIdRaw, out invitationId))
        {
            Console.WriteLine($"[CANCEL] INVALID GUIDS. OrgId: {orgIdRaw}, InvId: {invitationIdRaw}");
            AddError("Invalid parameters");
            ThrowIfAnyErrors();
        }

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
