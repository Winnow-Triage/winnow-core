using FastEndpoints;
using FluentValidation;
using MediatR;

namespace Winnow.API.Features.Auth.ResetPassword;

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class ResetPasswordEndpoint(IMediator mediator) : Endpoint<ResetPasswordRequest>
{
    public override void Configure()
    {
        Post("/auth/reset-password");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Reset a user's password using a token";
            s.Description = "Validates the reset token and updates the user's password.";
            s.Response(200, "Password reset successful");
            s.Response(400, "Invalid token or request");
        });
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(req.Email, req.Token, req.NewPassword);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.Message == "Invalid request")
            {
                ThrowError("Invalid request");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    AddError(error);
                }
                ThrowIfAnyErrors();
            }
        }

        await Send.OkAsync(new { Message = result.Message }, ct);
    }
}
