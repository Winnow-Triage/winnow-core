using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Shared;

public class ContactRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ContactEndpoint(IMediator mediator) : Endpoint<ContactRequest>
{
    public override void Configure()
    {
        Post("/contact");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Submit a contact form";
            s.Description = "Receives contact form submissions from the marketing site and sends an email notification.";
        });
    }

    public override async Task HandleAsync(ContactRequest req, CancellationToken ct)
    {
        var command = new SubmitContactFormCommand(req.FirstName, req.LastName, req.Email, req.Message);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(cancellation: ct);
    }
}
