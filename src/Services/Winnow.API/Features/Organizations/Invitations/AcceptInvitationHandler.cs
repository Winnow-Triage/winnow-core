using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Emails;

namespace Winnow.API.Features.Organizations.Invitations;

public record AcceptInvitationCommand(string Token, string FirstName, string LastName, string Password) : IRequest<AcceptInvitationResult>;

public record AcceptInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null, IEnumerable<string>? IdentityErrors = null);

public class AcceptInvitationHandler(
    WinnowDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : IRequestHandler<AcceptInvitationCommand, AcceptInvitationResult>
{
    public async Task<AcceptInvitationResult> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            return new AcceptInvitationResult(false, "Invitation not found", 404);
        }

        if (invitation.IsExpired())
        {
            return new AcceptInvitationResult(false, "Invitation expired", 410);
        }

        var emailStr = invitation.Email.Value;
        var user = new ApplicationUser
        {
            UserName = emailStr,
            Email = emailStr,
            FullName = $"{request.FirstName} {request.LastName}",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AcceptInvitationResult(false, "Failed to create user", 400, result.Errors.Select(e => e.Description));
        }

        var member = new Winnow.API.Domain.Organizations.OrganizationMember(
            invitation.OrganizationId,
            user.Id,
            invitation.RoleId);

        db.OrganizationMembers.Add(member);

        foreach (var teamId in invitation.InitialTeamIds)
        {
            db.TeamMembers.Add(new Winnow.API.Domain.Teams.TeamMember(teamId, user.Id));
        }

        foreach (var projectId in invitation.InitialProjectIds)
        {
            db.ProjectMembers.Add(new Winnow.API.Domain.Projects.ProjectMember(projectId, user.Id));
        }

        invitation.Accept();

        db.OrganizationInvitations.Remove(invitation);

        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
        }
        catch
        {
            // Ignore email errors
        }

        return new AcceptInvitationResult(true);
    }
}
