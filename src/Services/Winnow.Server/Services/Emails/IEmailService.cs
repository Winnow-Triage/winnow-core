using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Winnow.Server.Services.Emails;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);

    // Template helpers
    Task SendWelcomeEmailAsync(string to, string userName);

    Task SendEmailVerificationAsync(string to, Uri actionUrl);

    Task SendPasswordResetAsync(string to, Uri resetUrl);


    Task SendOrganizationInviteAsync(string to, string orgName, Uri inviteLink);
}
