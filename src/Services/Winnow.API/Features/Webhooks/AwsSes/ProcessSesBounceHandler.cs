using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Webhooks.AwsSes;

// Fire and forget command (or we can just let it run async)
public record ProcessSesBounceCommand(string SesMessageJson) : IRequest;

public class ProcessSesBounceHandler(UserManager<ApplicationUser> userManager, ILogger<ProcessSesBounceHandler> logger) : IRequestHandler<ProcessSesBounceCommand>
{
    public async Task Handle(ProcessSesBounceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SesMessageJson)) return;

            using var document = JsonDocument.Parse(request.SesMessageJson);
            var root = document.RootElement;

            if (!IsBounceNotification(root)) return;

            var recipients = GetBouncedRecipients(root);
            await ProcessRecipientsAsync(recipients, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing SES bounce message.");
        }
    }

    private static bool IsBounceNotification(JsonElement root) =>
        root.TryGetProperty("notificationType", out var type) && type.GetString() == "Bounce";

    private static IEnumerable<JsonElement> GetBouncedRecipients(JsonElement root)
    {
        if (root.TryGetProperty("bounce", out var bounce) &&
            bounce.TryGetProperty("bouncedRecipients", out var recipients) &&
            recipients.ValueKind == JsonValueKind.Array)
        {
            return recipients.EnumerateArray();
        }
        return Enumerable.Empty<JsonElement>();
    }

    private async Task ProcessRecipientsAsync(IEnumerable<JsonElement> recipients, CancellationToken cancellationToken)
    {
        foreach (var recipient in recipients)
        {
            if (recipient.TryGetProperty("emailAddress", out var emailElement))
            {
                var email = emailElement.GetString();
                if (!string.IsNullOrEmpty(email))
                {
                    await MarkEmailAsBouncedAsync(email);
                }
            }
        }
    }

    private async Task MarkEmailAsBouncedAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            user.EmailBounced = true;
            user.BouncedAt = DateTime.UtcNow;
            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                logger.LogInformation("Marked email as bounced for user {UserId} ({Email})", user.Id, email);
            }
            else
            {
                logger.LogWarning("Failed to update bounce status for user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
