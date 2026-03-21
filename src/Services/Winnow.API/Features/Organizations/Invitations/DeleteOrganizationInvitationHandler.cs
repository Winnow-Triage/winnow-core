using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.Invitations;

[RequirePermission("members:manage")]
public record DeleteOrganizationInvitationCommand(Guid CurrentOrganizationId, Guid InvitationId, string CurrentUserId) : IRequest<DeleteOrganizationInvitationResult>, IOrgScopedRequest;

public record DeleteOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DeleteOrganizationInvitationHandler(WinnowDbContext db) : IRequestHandler<DeleteOrganizationInvitationCommand, DeleteOrganizationInvitationResult>
{
    public async Task<DeleteOrganizationInvitationResult> Handle(DeleteOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId && (om.Role.Name == "Owner" || om.Role.Name == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new DeleteOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var invitation = await db.OrganizationInvitations
            .FirstOrDefaultAsync(oi => oi.Id == request.InvitationId && oi.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (invitation == null)
        {
            return new DeleteOrganizationInvitationResult(false, "Invitation not found", 404);
        }

        db.OrganizationInvitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteOrganizationInvitationResult(true);
    }
}
