using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Auth.ResendVerification;

public sealed class ResendVerificationEndpoint(IMediator mediator) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/resend-verification");
        Summary(s =>
        {
            s.Summary = "Resend email verification token";
            s.Description = "Generates a fresh email confirmation token and sends it to the authenticated user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var command = new ResendVerificationCommand(userId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            if (result.StatusCode == 429)
            {
                await Send.ErrorsAsync(429, ct); // Rate limit reached
                return;
            }
        }

        await Send.OkAsync(new { result.Message }, ct);
    }
}
