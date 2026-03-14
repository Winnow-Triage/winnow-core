using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Organizations.Invitations;

public class ResendOrganizationInvitationRequest
{
    public Guid OrgId { get; set; }
    public Guid InvitationId { get; set; }
}

public sealed class ResendOrganizationInvitationEndpoint(IMediator mediator) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/organizations/{orgId}/invitations/{invitationId}/resend");
        Policies("RequireVerifiedEmail");
        Summary(s =>
        {
            s.Summary = "Resend a pending invitation";
            s.Description = "Regenerates the token and re-sends the invite email. Email verification required.";
        });
        Description(d => d.WithDescription("Email verification required to perform this action."));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var invitationIdRaw = Route<string>("invitationId");
        Console.WriteLine($"[RESEND] Attempting to resend invitation. OrgId (raw): {orgIdRaw}, InvId (raw): {invitationIdRaw}");

        Guid orgId = Guid.Empty;
        Guid invitationId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId) || !Guid.TryParse(invitationIdRaw, out invitationId))
        {
            Console.WriteLine($"[RESEND] INVALID GUIDS. OrgId: {orgIdRaw}, InvId: {invitationIdRaw}");
            AddError("Invalid parameters");
            ThrowIfAnyErrors();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var command = new ResendOrganizationInvitationCommand(orgId, invitationId, userId);
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

        await Send.OkAsync(new { Message = "Invitation resent successfully" }, ct);
    }
}
