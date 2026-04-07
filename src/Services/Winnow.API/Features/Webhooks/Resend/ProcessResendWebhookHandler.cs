using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Domain.Integrations;
using Winnow.Integrations.Domain;
using System.Text.Json;

namespace Winnow.API.Features.Webhooks.Resend;

public record ProcessResendWebhookCommand(string EventType, JsonElement Data) : IRequest;

public class ProcessResendWebhookHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    ILogger<ProcessResendWebhookHandler> logger) : IRequestHandler<ProcessResendWebhookCommand>
{
    public async Task Handle(ProcessResendWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.EventType == "email.bounced")
            {
                await HandleBounceAsync(request.Data, cancellationToken);
            }
            else if (request.EventType == "email.complained")
            {
                await HandleComplaintAsync(request.Data, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Resend webhook event {EventType}", request.EventType);
        }
    }

    private async Task HandleBounceAsync(JsonElement data, CancellationToken ct)
    {
        // Resend payload for email.bounced contains the recipient
        if (!data.TryGetProperty("to", out var toArr) || toArr.ValueKind != JsonValueKind.Array || toArr.GetArrayLength() == 0)
        {
            return;
        }

        foreach (var toElem in toArr.EnumerateArray())
        {
            var email = toElem.GetString();
            if (string.IsNullOrEmpty(email)) continue;

            logger.LogWarning("Email bounced: {Email}", email);
            await MarkEmailAsFailedAsync(email, "Permanent Bounce", ct);
        }
    }

    private async Task HandleComplaintAsync(JsonElement data, CancellationToken ct)
    {
        if (!data.TryGetProperty("to", out var toArr) || toArr.ValueKind != JsonValueKind.Array || toArr.GetArrayLength() == 0)
        {
            return;
        }

        foreach (var toElem in toArr.EnumerateArray())
        {
            var email = toElem.GetString();
            if (string.IsNullOrEmpty(email)) continue;

            logger.LogCritical("Email complaint received: {Email}", email);
            await MarkEmailAsFailedAsync(email, "Spam Complaint", ct);
        }
    }

    private async Task MarkEmailAsFailedAsync(string email, string reason, CancellationToken ct)
    {
        // 1. Check if it's an ApplicationUser
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            user.EmailBounced = true;
            user.BouncedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            logger.LogInformation("Marked user {UserId} as bounced due to {Reason}", user.Id, reason);
        }

        // 2. Check if it's a Project Integration (Email alert)
        // Since Config is a JSON column, we filter client-side for now or use JSON functions if using Postgres/SQL Server.
        // For simplicity and compatibility, we'll fetch integrations with Provider == "email"
        var emailIntegrations = await dbContext.Integrations
            .Where(i => i.Provider == "email" && i.IsActive)
            .ToListAsync(ct);

        foreach (var integration in emailIntegrations)
        {
            if (integration.Config is EmailConfig emailConfig && emailConfig.RecipientEmail == email)
            {
                // Mark as unverified or failed
                var updatedConfig = emailConfig with { IsVerified = false };
                integration.UpdateConfig(updatedConfig);

                logger.LogWarning("Disabled email integration {IntegrationId} for project {ProjectId} because of {Reason}",
                    integration.Id, integration.ProjectId, reason);
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
