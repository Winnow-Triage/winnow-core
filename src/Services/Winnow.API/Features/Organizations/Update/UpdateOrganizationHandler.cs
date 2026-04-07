using Winnow.API.Features.Organizations.Get;
using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;
using Winnow.API.Domain.Common;

namespace Winnow.API.Features.Organizations.Update;

[RequirePermission("settings:manage")]
public record UpdateOrganizationCommand(
    Guid CurrentOrganizationId,
    string Name,
    bool? ToxicityFilterEnabled = null,
    ToxicityThresholdsDto? ToxicityLimits = null,
    AIConfigurationDto? AIConfig = null,
    NotificationSettingsDto? Notifications = null) : IRequest<UpdateOrganizationResult>, IOrgScopedRequest;

public record UpdateOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateOrganizationHandler(WinnowDbContext db) : IRequestHandler<UpdateOrganizationCommand, UpdateOrganizationResult>
{
    public async Task<UpdateOrganizationResult> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new UpdateOrganizationResult(false, null, "Organization not found", 404);
        }

        organization.Rename(request.Name);

        if (request.ToxicityFilterEnabled.HasValue)
        {
            organization.Settings.ToggleToxicityFilter(request.ToxicityFilterEnabled.Value);
        }

        if (request.ToxicityLimits != null)
        {
            var newLimits = new Winnow.API.Domain.Organizations.ValueObjects.ToxicityThresholds(
                Profanity: request.ToxicityLimits.Profanity,
                HateSpeech: request.ToxicityLimits.HateSpeech,
                Violence: request.ToxicityLimits.Violence,
                Insult: request.ToxicityLimits.Insult,
                Harassment: request.ToxicityLimits.Harassment,
                Sexual: request.ToxicityLimits.Sexual,
                Graphic: request.ToxicityLimits.Graphic,
                Overall: request.ToxicityLimits.Overall
            );
            organization.Settings.UpdateToxicityLimits(newLimits);
        }

        if (request.AIConfig != null)
        {
            var newAiConfig = new Winnow.API.Domain.Organizations.ValueObjects.AIConfiguration(
                Tokenizer: request.AIConfig.Tokenizer,
                SummaryAgent: request.AIConfig.SummaryAgent,
                CustomProviders: request.AIConfig.CustomProviders
                    .Select(p => new Winnow.API.Domain.Organizations.ValueObjects.CustomAIProvider(
                        p.Name, p.Type, p.ProviderId, p.Provider, p.ApiKey, p.ModelId)).ToList()
            );
            organization.Settings.UpdateAIConfiguration(newAiConfig);
        }

        if (request.Notifications != null)
        {
            var newNotifications = new NotificationSettings(
                request.Notifications.VolumeThreshold,
                request.Notifications.CriticalityThreshold
            );
            organization.Settings.UpdateNotificationThresholds(newNotifications);
        }

        await db.SaveChangesAsync(cancellationToken);

        var data = new CurrentOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name ?? "Free",
            CreatedAt = organization.CreatedAt,
            ToxicityFilterEnabled = organization.Settings.ToxicityFilterEnabled,
            ToxicityLimits = new ToxicityThresholdsDto
            {
                Profanity = organization.Settings.ToxicityLimits.Profanity,
                HateSpeech = organization.Settings.ToxicityLimits.HateSpeech,
                Violence = organization.Settings.ToxicityLimits.Violence,
                Insult = organization.Settings.ToxicityLimits.Insult,
                Harassment = organization.Settings.ToxicityLimits.Harassment,
                Sexual = organization.Settings.ToxicityLimits.Sexual,
                Graphic = organization.Settings.ToxicityLimits.Graphic,
                Overall = organization.Settings.ToxicityLimits.Overall
            },
            AIConfig = new AIConfigurationDto
            {
                Tokenizer = organization.Settings.AIConfig.Tokenizer,
                SummaryAgent = organization.Settings.AIConfig.SummaryAgent,
                CustomProviders = organization.Settings.AIConfig.AllCustomProviders
                    .Select(p => new CustomAIProviderDto
                    {
                        Name = p.Name,
                        Type = p.Type,
                        ProviderId = p.ProviderId,
                        Provider = p.Provider,
                        ApiKey = p.ApiKey,
                        ModelId = p.ModelId
                    }).ToList()
            },
            Notifications = new NotificationSettingsDto
            {
                VolumeThreshold = organization.Settings.Notifications.VolumeThreshold,
                CriticalityThreshold = organization.Settings.Notifications.CriticalityThreshold
            }
        };

        return new UpdateOrganizationResult(true, data);
    }
}
