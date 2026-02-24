using System.Net.Mail;
using System.Threading.Tasks;

namespace Winnow.Server.Services.Emails;

public class LocalEmailService : ILocalEmailService
{
    public async Task SendInvitationEmailAsync(string toEmail, string orgName, string inviteLink)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Emails", "Invitation.html");

        // Fallback for development if base directory doesn't contain the file (e.g. not copied yet)
        if (!File.Exists(templatePath))
        {
            // Try to find it in the project source during local development
            var projectRoot = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectRoot) && !File.Exists(Path.Combine(projectRoot, "Winnow.Server.csproj")))
            {
                projectRoot = Path.GetDirectoryName(projectRoot);
            }

            if (!string.IsNullOrEmpty(projectRoot))
            {
                templatePath = Path.Combine(projectRoot, "Resources", "Emails", "Invitation.html");
            }
        }

        string body;
        if (File.Exists(templatePath))
        {
            body = await File.ReadAllTextAsync(templatePath);
            body = body.Replace("{{OrgName}}", orgName)
                       .Replace("{{InviteLink}}", inviteLink);
        }
        else
        {
            // Simple fallback if template is missing
            body = $"<h1>Join {orgName}</h1><p>Invitation link: <a href='{inviteLink}'>{inviteLink}</a></p>";
        }

        using var client = new SmtpClient("localhost", 1025);
        client.EnableSsl = false;

        var mailMessage = new MailMessage
        {
            From = new MailAddress("no-reply@winnowtriage.com", "Winnow"),
            Subject = $"Join {orgName} on Winnow",
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }
}
