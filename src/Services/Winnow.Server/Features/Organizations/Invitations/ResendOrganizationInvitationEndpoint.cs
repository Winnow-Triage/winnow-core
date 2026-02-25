using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Organizations.Invitations;

public class ResendOrganizationInvitationRequest
{
    public Guid OrgId { get; set; }
    public Guid InvitationId { get; set; }
}

public sealed class ResendOrganizationInvitationEndpoint(
    WinnowDbContext db,
    ILocalEmailService emailService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/organizations/{orgId:guid}/invitations/{invitationId:guid}/resend");
        Summary(s =>
        {
            s.Summary = "Resend a pending invitation";
            s.Description = "Regenerates the token and re-sends the invite email.";
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
            .Include(oi => oi.Organization)
            .FirstOrDefaultAsync(oi => oi.Id == invitationId && oi.OrganizationId == orgId, ct);

        if (invitation == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Regenerate token to invalidate old links
        var oldToken = invitation.Token;
        invitation.Token = Guid.NewGuid().ToString("N");

        // Extend expiration
        invitation.ExpiresAt = DateTime.UtcNow.AddHours(24);

        Console.WriteLine($"[RESEND] Regenerated token for invitation {invitation.Id}: {oldToken} -> {invitation.Token}");

        await db.SaveChangesAsync(ct);

        // Resend email
        var inviteLink = $"http://localhost:5173/accept-invite?token={invitation.Token}";
        await emailService.SendInvitationEmailAsync(invitation.Email, invitation.Organization.Name, inviteLink);

        await Send.OkAsync(new { Message = "Invitation resent successfully" }, ct);
    }
}
