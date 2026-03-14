using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Auth.ForgotPassword;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ForgotPasswordEndpoint(IMediator mediator) : Endpoint<ForgotPasswordRequest>
{
    public override void Configure()
    {
        Post("/auth/forgot-password");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Request a password reset email";
            s.Description = "Generates a password reset token and sends it to the user's email.";
            s.Response(200, "If the email exists, a reset link will be sent.");
            s.Response(400, "Validation failed");
        });
    }

    public override async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(req.Email);
        await mediator.Send(command, ct);

        await Send.OkAsync(new { Message = "If an account with that email exists, we have sent a password reset link." }, ct);
    }
}
