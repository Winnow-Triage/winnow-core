using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Organizations.Create;

public record CreateOrganizationInvitationCommand(string UserId, Guid OrgId, string Email, string Role, List<Guid> TeamIds, List<Guid> ProjectIds) : IRequest<CreateOrganizationInvitationResult>;

public record CreateOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class CreateOrganizationInvitationHandler(
    WinnowDbContext db,
    IEmailService emailService) : IRequestHandler<CreateOrganizationInvitationCommand, CreateOrganizationInvitationResult>
{
    public async Task<CreateOrganizationInvitationResult> Handle(CreateOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.OrgId && om.UserId == request.UserId && (om.Role == "owner" || om.Role == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new CreateOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var org = await db.Organizations.FindAsync([request.OrgId], cancellationToken);
        if (org == null)
        {
            return new CreateOrganizationInvitationResult(false, "Organization not found", 404);
        }

        var token = Guid.NewGuid().ToString("N");
        var invitation = new Winnow.Server.Domain.Organizations.OrganizationInvitation(
            request.OrgId,
            new Winnow.Server.Domain.Common.Email(request.Email),
            request.Role,
            token,
            request.TeamIds,
            request.ProjectIds);

        db.OrganizationInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        var inviteLink = new Uri($"http://localhost:5173/accept-invite?token={token}");
        await emailService.SendOrganizationInviteAsync(request.Email, org.Name, inviteLink);

        return new CreateOrganizationInvitationResult(true);
    }
}
