using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Auth.VerifyEmail;

public class VerifyEmailRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailResponse
{
    public string Message { get; set; } = string.Empty;
}

public sealed class VerifyEmailEndpoint(IMediator mediator)
    : Endpoint<VerifyEmailRequest, VerifyEmailResponse>
{
    public override void Configure()
    {
        Get("/auth/verify-email");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Verify user email";
            s.Description = "Confirms a user's email address using a token sent via email.";
            s.Response<VerifyEmailResponse>(200, "Email verified successfully");
            s.Response(400, "Verification failed");
        });
    }

    public override async Task HandleAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        var command = new VerifyEmailCommand(req.UserId, req.Token);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.Message == "User not found.")
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error);
            }
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(new VerifyEmailResponse
        {
            Message = result.Message
        }, ct);
    }
}
