using FastEndpoints;
using MediatR;
using Winnow.API.Infrastructure.MultiTenancy;

namespace Winnow.API.Features.Organizations.Get;

public class ToxicityThresholdsDto
{
    public float Profanity { get; set; }
    public float HateSpeech { get; set; }
    public float Violence { get; set; }
    public float Insult { get; set; }
    public float Harassment { get; set; }
    public float Sexual { get; set; }
    public float Graphic { get; set; }
    public float Overall { get; set; }
}

public class CustomAIProviderDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}

public class AIConfigurationDto
{
    public string Tokenizer { get; set; } = "Default";
    public string SummaryAgent { get; set; } = "Default";
    public List<CustomAIProviderDto> CustomProviders { get; set; } = [];
}

public class CurrentOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool ToxicityFilterEnabled { get; set; }
    public ToxicityThresholdsDto ToxicityLimits { get; set; } = new();
    public AIConfigurationDto AIConfig { get; set; } = new();
}

public sealed class GetCurrentOrganizationEndpoint(
    IMediator mediator,
    ITenantContext tenantContext)
    : EndpointWithoutRequest<CurrentOrganizationResponse>
{
    public override void Configure()
    {
        Get("/organizations/current");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var query = new GetCurrentOrganizationQuery(tenantContext.CurrentOrganizationId.Value);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, cancellation: ct);
    }
}
