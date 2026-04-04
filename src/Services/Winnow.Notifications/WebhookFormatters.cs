using Winnow.Contracts;

namespace Winnow.Notifications;

public class DiscordWebhookFormatter : IWebhookFormatter
{
    public NotificationProvider Provider => NotificationProvider.Discord;

    public object Format(SendWebhookNotificationCommand command)
    {
        // Discord Embeds support decimal color
        int? colorValue = null;
        if (!string.IsNullOrWhiteSpace(command.Color) && command.Color.StartsWith('#'))
        {
            if (int.TryParse(command.Color[1..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
            {
                colorValue = hex;
            }
        }

        return new
        {
            content = string.IsNullOrWhiteSpace(command.Title) ? command.Message : null,
            embeds = new[]
            {
                new
                {
                    title = command.Title,
                    description = command.Message,
                    color = colorValue,
                    url = command.DetailUrl?.ToString(),
                    timestamp = DateTime.UtcNow.ToString("O")
                }
            }
        };
    }
}

public class SlackWebhookFormatter : IWebhookFormatter
{
    public NotificationProvider Provider => NotificationProvider.Slack;

    public object Format(SendWebhookNotificationCommand command)
    {
        var blocks = new List<object>();

        if (!string.IsNullOrWhiteSpace(command.Title))
        {
            blocks.Add(new
            {
                type = "header",
                text = new { type = "plain_text", text = command.Title }
            });
        }

        blocks.Add(new
        {
            type = "section",
            text = new { type = "mrkdwn", text = command.Message }
        });

        if (command.DetailUrl != null)
        {
            blocks.Add(new
            {
                type = "actions",
                elements = new[]
                {
                    new
                    {
                        type = "button",
                        text = new { type = "plain_text", text = "View Details" },
                        url = command.DetailUrl.ToString(),
                        style = "primary"
                    }
                }
            });
        }

        return new { blocks };
    }
}

public class MicrosoftTeamsWebhookFormatter : IWebhookFormatter
{
    public NotificationProvider Provider => NotificationProvider.MicrosoftTeams;

    public object Format(SendWebhookNotificationCommand command)
    {
        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.2",
                        body = new object[]
                        {
                            new { type = "TextBlock", text = command.Title, weight = "bolder", size = "medium" },
                            new { type = "TextBlock", text = command.Message, wrap = true }
                        },
                        actions = command.DetailUrl == null ? null : new[]
                        {
                            new
                            {
                                type = "Action.OpenUrl",
                                title = "View Details",
                                url = command.DetailUrl.ToString()
                            }
                        }
                    }
                }
            }
        };
    }
}
