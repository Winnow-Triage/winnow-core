using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Winnow.Server.Features.Organizations.Invitations;

public class GetInvitationDetailsRequest
{
    public string Token { get; set; } = string.Empty;
}

public class GetInvitationDetailsResponse
{
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public sealed class GetInvitationDetailsEndpoint(IMediator mediator) : Endpoint<GetInvitationDetailsRequest, GetInvitationDetailsResponse>
{
    public override void Configure()
    {
        Get("/invitations/{token}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetInvitationDetailsRequest req, CancellationToken ct)
    {
        var query = new GetInvitationDetailsQuery(req.Token);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            if (result.StatusCode == 410)
            {
                await Send.ErrorsAsync(410, ct); // Gone
                return;
            }

            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
