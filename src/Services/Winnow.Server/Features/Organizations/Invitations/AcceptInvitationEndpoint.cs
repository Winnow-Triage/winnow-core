using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Invitations;

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AcceptInvitationEndpoint(
    WinnowDbContext db,
    UserManager<ApplicationUser> userManager) : Endpoint<AcceptInvitationRequest>
{
    public override void Configure()
    {
        Post("/invitations/{token}/accept");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AcceptInvitationRequest req, CancellationToken ct)
    {
        var invitation = await db.OrganizationInvitations
            .Include(oi => oi.Organization)
            .FirstOrDefaultAsync(oi => oi.Token == req.Token, ct);

        if (invitation == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            await Send.ErrorsAsync(410, ct); // Gone
            return;
        }

        // 1. Create the User record
        var user = new ApplicationUser
        {
            UserName = invitation.Email,
            Email = invitation.Email,
            FullName = $"{req.FirstName} {req.LastName}",
            EmailConfirmed = true // They came from a verified invite link
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // 2. Assign to Organization
        var member = new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = user.Id,
            Role = invitation.Role
        };

        db.OrganizationMembers.Add(member);

        // 3. Assign to Initial Teams
        foreach (var teamId in invitation.InitialTeamIds)
        {
            db.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = user.Id
            });
        }

        // 4. Assign to Initial Projects
        foreach (var projectId in invitation.InitialProjectIds)
        {
            db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = user.Id
            });
        }

        // 5. Delete the invitation
        db.OrganizationInvitations.Remove(invitation);

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new { Message = "Invitation accepted successfully" }, ct);
    }
}
