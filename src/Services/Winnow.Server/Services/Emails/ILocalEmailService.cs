using System.Threading.Tasks;

namespace Winnow.Server.Services.Emails;

public interface ILocalEmailService
{
    Task SendInvitationEmailAsync(string toEmail, string orgName, string inviteLink);
}
