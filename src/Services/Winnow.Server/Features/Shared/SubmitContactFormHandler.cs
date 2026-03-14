using MediatR;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Shared;

public record SubmitContactFormCommand(string FirstName, string LastName, string Email, string Message) : IRequest<SubmitContactFormResult>;

public record SubmitContactFormResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class SubmitContactFormHandler(IEmailService emailService, IConfiguration config) : IRequestHandler<SubmitContactFormCommand, SubmitContactFormResult>
{
    public async Task<SubmitContactFormResult> Handle(SubmitContactFormCommand request, CancellationToken cancellationToken)
    {
        var supportEmail = config["EmailSettings:SupportEmail"] ?? "support@winnow-triage.com";

        var emailBody = $@"
            <h3>New Contact Form Submission</h3>
            <p><strong>From:</strong> {request.FirstName} {request.LastName} ({request.Email})</p>
            <p><strong>Message:</strong></p>
            <p>{request.Message}</p>
        ";

        await emailService.SendEmailAsync(
            supportEmail,
            $"Contact Form: {request.FirstName} {request.LastName}",
            emailBody);

        return new SubmitContactFormResult(true);
    }
}
