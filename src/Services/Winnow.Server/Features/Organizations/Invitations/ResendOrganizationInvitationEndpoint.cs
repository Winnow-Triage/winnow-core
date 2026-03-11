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
    IEmailService emailService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/organizations/{orgId}/invitations/{invitationId}/resend");
        Policies("RequireVerifiedEmail");
        Summary(s =>
        {
            s.Summary = "Resend a pending invitation";
            s.Description = "Regenerates the token and re-sends the invite email. Email verification required.";
        });
        Description(d => d.WithDescription("Email verification required to perform this action."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var invitationIdRaw = Route<string>("invitationId");
        Console.WriteLine($"[RESEND] Attempting to resend invitation. OrgId (raw): {orgIdRaw}, InvId (raw): {invitationIdRaw}");

        Guid orgId = Guid.Empty;
        Guid invitationId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId) || !Guid.TryParse(invitationIdRaw, out invitationId))
        {
            Console.WriteLine($"[RESEND] INVALID GUIDS. OrgId: {orgIdRaw}, InvId: {invitationIdRaw}");
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

        var organizationName = await db.Organizations
            .Where(o => o.Id == orgId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct) ?? "Organization";

        // Regenerate token and extend expiration using domain method
        var oldToken = invitation.Token;
        invitation.Resend(Guid.NewGuid().ToString("N"));

        Console.WriteLine($"[RESEND] Regenerated token for invitation {invitation.Id}: {oldToken} -> {invitation.Token}");

        await db.SaveChangesAsync(ct);

        // Resend email
        var inviteLink = new Uri($"http://localhost:5173/accept-invite?token={invitation.Token}");
        await emailService.SendOrganizationInviteAsync(invitation.Email.Value, organizationName, inviteLink);

        await Send.OkAsync(new { Message = "Invitation resent successfully" }, ct);
    }
}
