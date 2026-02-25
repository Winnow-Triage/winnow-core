using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Organizations;

public class CreateOrganizationInvitationRequest
{
    public Guid OrgId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public List<Guid> TeamIds { get; set; } = [];
    public List<Guid> ProjectIds { get; set; } = [];
}

public sealed class CreateOrganizationInvitationEndpoint(
    WinnowDbContext db,
    ILocalEmailService emailService) : Endpoint<CreateOrganizationInvitationRequest>
{
    public override void Configure()
    {
        Post("/organizations/{orgId}/invitations");
    }

    public override async Task HandleAsync(CreateOrganizationInvitationRequest req, CancellationToken ct)
    {
        // 1. Validate the user has admin rights for this organization
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == req.OrgId && om.UserId == userId && (om.Role == "owner" || om.Role == "Admin"), ct);

        if (!isOwner)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        // 2. Generate the invitation
        var org = await db.Organizations.FindAsync([req.OrgId], ct);
        if (org == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var token = Guid.NewGuid().ToString("N"); // Secure enough for local/MVP
        var invitation = new OrganizationInvitation
        {
            OrganizationId = req.OrgId,
            Email = req.Email,
            Role = req.Role,
            Token = token,
            InitialTeamIds = req.TeamIds,
            InitialProjectIds = req.ProjectIds
        };

        db.OrganizationInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);

        // 3. Send the email
        var inviteLink = $"http://localhost:5173/accept-invite?token={token}";
        await emailService.SendInvitationEmailAsync(req.Email, org.Name, inviteLink);

        await Send.OkAsync(new { Message = "Invitation sent successfully" }, ct);
    }
}
