using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Winnow.API.Infrastructure.Configuration;

namespace Winnow.API.Services.Emails;

public class AwsSesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly EmailSettings _settings;

    public AwsSesEmailService(IAmazonSimpleEmailService sesClient, EmailSettings settings)
    {
        _sesClient = sesClient;
        _settings = settings;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var sendRequest = new SendEmailRequest
        {
            Source = $"{_settings.FromName} <{_settings.FromAddress}>",
            Destination = new Destination
            {
                ToAddresses = [to]
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content(htmlBody)
                }
            }
        };

        await _sesClient.SendEmailAsync(sendRequest);
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
