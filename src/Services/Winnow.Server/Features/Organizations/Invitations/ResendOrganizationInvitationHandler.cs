using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Organizations.Invitations;

public record ResendOrganizationInvitationCommand(Guid OrganizationId, Guid InvitationId, string CurrentUserId) : IRequest<ResendOrganizationInvitationResult>;

public record ResendOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class ResendOrganizationInvitationHandler(
    WinnowDbContext db,
    IEmailService emailService) : IRequestHandler<ResendOrganizationInvitationCommand, ResendOrganizationInvitationResult>
{
    public async Task<ResendOrganizationInvitationResult> Handle(ResendOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.OrganizationId && om.UserId == request.CurrentUserId && (om.Role == "owner" || om.Role == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new ResendOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Id == request.InvitationId && oi.OrganizationId == request.OrganizationId, cancellationToken);

        if (invitation == null)
        {
            return new ResendOrganizationInvitationResult(false, "Invitation not found", 404);
        }

        var organizationName = await db.Organizations
            .Where(o => o.Id == request.OrganizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Organization";

        invitation.Resend(Guid.NewGuid().ToString("N"));

        await db.SaveChangesAsync(cancellationToken);

        var inviteLink = new Uri($"http://localhost:5173/accept-invite?token={invitation.Token}");
        await emailService.SendOrganizationInviteAsync(invitation.Email.Value, organizationName, inviteLink);

        return new ResendOrganizationInvitationResult(true);
    }
}
