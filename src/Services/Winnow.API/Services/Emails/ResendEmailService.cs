using Resend;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Emails;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resendClient;
    private readonly EmailSettings _settings;

    public ResendEmailService(IResend resendClient, EmailSettings settings)
    {
        _resendClient = resendClient;
        _settings = settings;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromAddress
            : $"{_settings.FromName} <{_settings.FromAddress}>";

        var message = new EmailMessage
        {
            From = from,
            Subject = subject,
            HtmlBody = htmlBody
        };
        message.To.Add(to);

        await _resendClient.EmailSendAsync(message);
    }

    public async Task SendWelcomeEmailAsync(string to, string userName)
    {
        var body = await LoadTemplateAsync("Welcome.html");
        body = body.Replace("{{UserName}}", userName);
        await SendEmailAsync(to, "Welcome to Winnow", body);
    }

    public async Task SendEmailVerificationAsync(string to, Uri actionUrl)
    {
        var body = await LoadTemplateAsync("Verification.html");
        body = body.Replace("{{ActionUrl}}", actionUrl.ToString());
        await SendEmailAsync(to, "Confirm your email address", body);
    }

    public async Task SendPasswordResetAsync(string to, Uri resetUrl)
    {
        var body = await LoadTemplateAsync("PasswordReset.html");
        body = body.Replace("{{ResetUrl}}", resetUrl.ToString());
        await SendEmailAsync(to, "Reset your password", body);
    }

    public async Task SendIntegrationVerificationAsync(string to, string projectName, Uri verifyUrl)
    {
        var body = await LoadTemplateAsync("Verification.html");
        body = body.Replace("{{ActionUrl}}", verifyUrl.ToString());
        await SendEmailAsync(to, $"Verify Alert Destination for {projectName}", body);
    }

    public async Task SendOrganizationInviteAsync(string to, string orgName, Uri inviteLink)
    {
        var body = await LoadTemplateAsync("Invitation.html");
        body = body.Replace("{{OrgName}}", orgName)
                   .Replace("{{InviteLink}}", inviteLink.ToString());
        await SendEmailAsync(to, $"Join {orgName} on Winnow", body);
    }

    private async Task<string> LoadTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Emails", templateName);

        if (!File.Exists(templatePath))
        {
            var projectRoot = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectRoot) && !File.Exists(Path.Combine(projectRoot, "Winnow.API.csproj")))
            {
                projectRoot = Path.GetDirectoryName(projectRoot);
            }

            if (!string.IsNullOrEmpty(projectRoot))
            {
                templatePath = Path.Combine(projectRoot, "Resources", "Emails", templateName);
            }
        }

        if (File.Exists(templatePath))
        {
            return await File.ReadAllTextAsync(templatePath);
        }

        return $"<h1>Winnow Notification</h1><p>Template {templateName} not found.</p>";
    }
}
