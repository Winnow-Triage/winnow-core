using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Organizations.Invitations;

public sealed class DeleteOrganizationInvitationEndpoint(IMediator mediator)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/organizations/{orgId}/invitations/{invitationId}");
        Summary(s =>
        {
            s.Summary = "Delete a pending invitation";
            s.Description = "Deletes an invitation that has not been accepted yet.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var invitationIdRaw = Route<string>("invitationId");
        Console.WriteLine($"[CANCEL] Attempting to delete invitation. OrgId (raw): {orgIdRaw}, InvId (raw): {invitationIdRaw}");

        Guid orgId = Guid.Empty;
        Guid invitationId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId) || !Guid.TryParse(invitationIdRaw, out invitationId))
        {
            Console.WriteLine($"[CANCEL] INVALID GUIDS. OrgId: {orgIdRaw}, InvId: {invitationIdRaw}");
            AddError("Invalid parameters");
            ThrowIfAnyErrors();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var command = new DeleteOrganizationInvitationCommand(orgId, invitationId, userId);
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

        await Send.NoContentAsync(ct);
    }
}
