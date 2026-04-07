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

            if (root.TryGetProperty("notificationType", out var notificationType) && notificationType.GetString() == "Bounce")
            {
                if (root.TryGetProperty("bounce", out var bounceObj))
                {
                    if (bounceObj.TryGetProperty("bouncedRecipients", out var bouncedRecipients) && bouncedRecipients.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var recipient in bouncedRecipients.EnumerateArray())
                        {
                            if (recipient.TryGetProperty("emailAddress", out var emailAddressElem))
                            {
                                var email = emailAddressElem.GetString();
                                if (!string.IsNullOrEmpty(email))
                                {
                                    var user = await userManager.FindByEmailAsync(email);
                                    if (user != null)
                                    {
                                        user.EmailBounced = true;
                                        user.BouncedAt = DateTime.UtcNow;
                                        await userManager.UpdateAsync(user);
                                        logger.LogInformation("Marked email as bounced for user {UserId} ({Email})", user.Id, email);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing or processing SES bounce message JSON.");
            // Do not throw -> acknowledge SNS anyway.
        }
    }
}
