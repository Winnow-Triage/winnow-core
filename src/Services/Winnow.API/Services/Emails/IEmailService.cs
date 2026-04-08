using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Winnow.API.Services.Emails;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, string? fromAddress = null, string? fromName = null);

    // Template helpers
    Task SendWelcomeEmailAsync(string to, string userName);

    Task SendEmailVerificationAsync(string to, Uri actionUrl);

    Task SendPasswordResetAsync(string to, Uri resetUrl);

    Task SendIntegrationVerificationAsync(string to, string projectName, Uri verifyUrl);

    Task SendOrganizationInviteAsync(string to, string orgName, Uri inviteLink);
}
