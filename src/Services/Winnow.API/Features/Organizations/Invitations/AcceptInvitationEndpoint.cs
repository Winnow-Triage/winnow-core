using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Winnow.API.Features.Organizations.Invitations;

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AcceptInvitationValidator : Validator<AcceptInvitationRequest>
{
    public AcceptInvitationValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class AcceptInvitationEndpoint(
    IMediator mediator) : Endpoint<AcceptInvitationRequest>
{
    public override void Configure()
    {
        Post("/invitations/{token}/accept");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AcceptInvitationRequest req, CancellationToken ct)
    {
        Console.WriteLine($"[INVITE-ACCEPT] HandleAsync started for token: {req.Token}");

        var command = new AcceptInvitationCommand(req.Token, req.FirstName, req.LastName, req.Password);
        var result = await mediator.Send(command, ct);

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
            if (result.StatusCode == 400 && result.IdentityErrors != null)
            {
                foreach (var error in result.IdentityErrors)
                {
                    AddError(error);
                }
                await Send.ErrorsAsync(400, ct);
                return;
            }

            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(new { Message = "Invitation accepted successfully" }, ct);
    }
}
