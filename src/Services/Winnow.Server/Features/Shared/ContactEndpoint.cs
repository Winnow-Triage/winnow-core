using FastEndpoints;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Shared;

public class ContactRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ContactEndpoint(IEmailService emailService, IConfiguration config) : Endpoint<ContactRequest>
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
        var supportEmail = config["EmailSettings:SupportEmail"] ?? "support@winnow-triage.com";

        var emailBody = $@"
            <h3>New Contact Form Submission</h3>
            <p><strong>From:</strong> {req.FirstName} {req.LastName} ({req.Email})</p>
            <p><strong>Message:</strong></p>
            <p>{req.Message}</p>
        ";

        await emailService.SendEmailAsync(
            supportEmail,
            $"Contact Form: {req.FirstName} {req.LastName}",
            emailBody);

        await Send.OkAsync(cancellation: ct);
    }
}
