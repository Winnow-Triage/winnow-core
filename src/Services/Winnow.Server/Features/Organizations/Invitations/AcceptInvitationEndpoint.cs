using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Organizations.Invitations;

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AcceptInvitationValidator : Validator<AcceptInvitationRequest>
{
    public AcceptInvitationValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class AcceptInvitationEndpoint(
    WinnowDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : Endpoint<AcceptInvitationRequest>
{
    public override void Configure()
    {
        Post("/invitations/{token}/accept");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AcceptInvitationRequest req, CancellationToken ct)
    {
        Console.WriteLine($"[INVITE-ACCEPT] HandleAsync started for token: {req.Token}");
        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Token == req.Token, ct);

        if (invitation == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (invitation.IsExpired())
        {
            await Send.ErrorsAsync(410, ct); // Gone
            return;
        }

        // 1. Create the User record
        var emailStr = invitation.Email.Value;
        var user = new ApplicationUser
        {
            UserName = emailStr,
            Email = emailStr,
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
        var member = new Winnow.Server.Domain.Organizations.OrganizationMember(
            invitation.OrganizationId,
            user.Id,
            invitation.Role);

        db.OrganizationMembers.Add(member);

        // 3. Assign to Initial Teams
        foreach (var teamId in invitation.InitialTeamIds)
        {
            db.TeamMembers.Add(new Winnow.Server.Domain.Teams.TeamMember(teamId, user.Id));
        }

        // 4. Assign to Initial Projects
        foreach (var projectId in invitation.InitialProjectIds)
        {
            db.ProjectMembers.Add(new Winnow.Server.Domain.Projects.ProjectMember(projectId, user.Id));
        }

        // Mark invitation as accepted to emit events
        invitation.Accept();

        // 5. Delete the invitation
        db.OrganizationInvitations.Remove(invitation);

        await db.SaveChangesAsync(ct);

        // 6. Send Welcome Email
        try
        {
            Console.WriteLine($"[INVITE-ACCEPT] Sending welcome email to {user.Email}");
            await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
            Console.WriteLine($"[INVITE-ACCEPT] Welcome email sent to {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INVITE-ACCEPT] FAILED to send welcome email to {user.Email}: {ex.Message}");
            // Don't throw, we want registration to succeed even if email fails
        }

        await Send.OkAsync(new { Message = "Invitation accepted successfully" }, ct);
    }
}
