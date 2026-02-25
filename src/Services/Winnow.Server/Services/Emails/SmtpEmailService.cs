using System.Net.Mail;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Emails;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port);
        client.EnableSsl = _settings.Smtp.EnableSsl;

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
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
            while (!string.IsNullOrEmpty(projectRoot) && !File.Exists(Path.Combine(projectRoot, "Winnow.Server.csproj")))
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
