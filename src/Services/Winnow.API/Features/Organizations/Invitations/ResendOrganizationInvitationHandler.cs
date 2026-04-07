using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Emails;
using Winnow.API.Features.Shared;
using Microsoft.Extensions.Configuration;

namespace Winnow.API.Features.Organizations.Invitations;

[RequirePermission("members:manage")]
public record ResendOrganizationInvitationCommand(Guid CurrentOrganizationId, Guid InvitationId, string CurrentUserId) : IRequest<ResendOrganizationInvitationResult>, IOrgScopedRequest;

public record ResendOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class ResendOrganizationInvitationHandler(
    WinnowDbContext db,
    IEmailService emailService,
    IConfiguration config) : IRequestHandler<ResendOrganizationInvitationCommand, ResendOrganizationInvitationResult>
{
    public async Task<ResendOrganizationInvitationResult> Handle(ResendOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId && (om.Role.Name == "Owner" || om.Role.Name == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new ResendOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Id == request.InvitationId && oi.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (invitation == null)
        {
            return new ResendOrganizationInvitationResult(false, "Invitation not found", 404);
        }

        var organizationName = await db.Organizations
            .Where(o => o.Id == request.CurrentOrganizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Organization";

        invitation.Resend(Guid.NewGuid().ToString("N"));

        await db.SaveChangesAsync(cancellationToken);

        var appUrl = config["AppUrl"] ?? "https://app.winnowtriage.com";
        var inviteLink = new Uri($"{appUrl.TrimEnd('/')}/accept-invite?token={invitation.Token}");
        await emailService.SendOrganizationInviteAsync(invitation.Email.Value, organizationName, inviteLink);

        return new ResendOrganizationInvitationResult(true);
    }
}
