using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Winnow.Server.Features.Organizations.Create;

public class CreateOrganizationInvitationRequest
{
    public Guid OrgId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public List<Guid> TeamIds { get; set; } = [];
    public List<Guid> ProjectIds { get; set; } = [];
}

public sealed class CreateOrganizationInvitationEndpoint(
    IMediator mediator) : Endpoint<CreateOrganizationInvitationRequest>
{
    public override void Configure()
    {
        Post("/organizations/{orgId}/invitations");
        Policies("RequireVerifiedEmail");
        Description(d => d.WithDescription("Email verification required to perform this action."));
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

        var command = new CreateOrganizationInvitationCommand(userId, req.OrgId, req.Email, req.Role, req.TeamIds, req.ProjectIds);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 403)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(new { Message = "Invitation sent successfully" }, ct);
    }
}
