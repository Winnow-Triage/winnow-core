using Winnow.Contracts;

namespace Winnow.Notifications;

public interface IWebhookFormatter
{
    NotificationProvider Provider { get; }
    object Format(SendWebhookNotificationCommand command);
}
