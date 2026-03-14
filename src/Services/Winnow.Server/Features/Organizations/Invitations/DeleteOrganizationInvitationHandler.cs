using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Invitations;

public record DeleteOrganizationInvitationCommand(Guid OrganizationId, Guid InvitationId, string CurrentUserId) : IRequest<DeleteOrganizationInvitationResult>;

public record DeleteOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteOrganizationInvitationHandler(WinnowDbContext db) : IRequestHandler<DeleteOrganizationInvitationCommand, DeleteOrganizationInvitationResult>
{
    public async Task<DeleteOrganizationInvitationResult> Handle(DeleteOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.OrganizationId && om.UserId == request.CurrentUserId && (om.Role == "owner" || om.Role == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new DeleteOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Id == request.InvitationId && oi.OrganizationId == request.OrganizationId, cancellationToken);

        if (invitation == null)
        {
            return new DeleteOrganizationInvitationResult(false, "Invitation not found", 404);
        }

        db.OrganizationInvitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteOrganizationInvitationResult(true);
    }
}
