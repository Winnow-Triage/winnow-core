using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.Get;

[RequirePermission("organizations:read")]
public record GetCurrentOrganizationQuery(Guid CurrentOrganizationId) : IRequest<GetCurrentOrganizationResult>, IOrgScopedRequest;

public record GetCurrentOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetCurrentOrganizationHandler(WinnowDbContext db) : IRequestHandler<GetCurrentOrganizationQuery, GetCurrentOrganizationResult>
{
    public async Task<GetCurrentOrganizationResult> Handle(GetCurrentOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new GetCurrentOrganizationResult(false, null, "Organization not found", 404);
        }

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

        return new GetCurrentOrganizationResult(true, data);
    }
}
