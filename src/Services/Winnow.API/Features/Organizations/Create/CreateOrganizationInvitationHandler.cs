using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Emails;
using Winnow.API.Features.Shared;
using Microsoft.Extensions.Configuration;

namespace Winnow.API.Features.Organizations.Create;

[RequirePermission("members:manage")]
public record CreateOrganizationInvitationCommand(string UserId, Guid CurrentOrganizationId, string Email, Guid RoleId, List<Guid> TeamIds, List<Guid> ProjectIds) : IRequest<CreateOrganizationInvitationResult>, IOrgScopedRequest;

public record CreateOrganizationInvitationResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class CreateOrganizationInvitationHandler(
    WinnowDbContext db,
    IEmailService emailService,
    IConfiguration config) : IRequestHandler<CreateOrganizationInvitationCommand, CreateOrganizationInvitationResult>
{
    public async Task<CreateOrganizationInvitationResult> Handle(CreateOrganizationInvitationCommand request, CancellationToken cancellationToken)
    {
        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.UserId && (om.Role.Name == "Owner" || om.Role.Name == "Admin"), cancellationToken);

        if (!isOwner)
        {
            return new CreateOrganizationInvitationResult(false, "Forbidden", 403);
        }

        var org = await db.Organizations.FindAsync([request.CurrentOrganizationId], cancellationToken);
        if (org == null)
        {
            return new CreateOrganizationInvitationResult(false, "Organization not found", 404);
        }

        var isValidRole = await db.Roles
            .AnyAsync(r => r.Id == request.RoleId && (r.OrganizationId == request.CurrentOrganizationId || r.OrganizationId == null), cancellationToken);

        if (!isValidRole)
        {
            return new CreateOrganizationInvitationResult(false, "Invalid role", 400);
        }

        var token = Guid.NewGuid().ToString("N");
        var invitation = new Winnow.API.Domain.Organizations.OrganizationInvitation(
            request.CurrentOrganizationId,
            new Winnow.API.Domain.Common.Email(request.Email),
            request.RoleId,
            token,
            request.TeamIds,
            request.ProjectIds);

        db.OrganizationInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        var appUrl = config["AppUrl"] ?? "https://app.winnowtriage.com";
        var inviteLink = new Uri($"{appUrl.TrimEnd('/')}/accept-invite?token={token}");
        await emailService.SendOrganizationInviteAsync(request.Email, org.Name, inviteLink);

        return new CreateOrganizationInvitationResult(true);
    }
}
