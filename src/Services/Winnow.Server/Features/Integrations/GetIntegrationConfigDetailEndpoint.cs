using System.Text.Json;
using FastEndpoints;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Integrations;

public sealed class GetIntegrationConfigDetailEndpoint(WinnowDbContext db) : EndpointWithoutRequest<UpsertIntegrationConfigRequest>
{

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public override void Configure()
    {
        Get("/integrations/{Id}");
        Summary(s =>
        {
            s.Summary = "Get integration details";
            s.Description = "Retrieves the full configuration for an integration, including masked settings.";
            s.Response<UpsertIntegrationConfigRequest>(200, "Integration details");
            s.Response(404, "Integration not found");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("Id");
        var integration = await db.Integrations.FindAsync([id], ct);

        if (integration == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Serialize Config to JSON (secrets will be masked by the domain model)
        var maskedJson = JsonSerializer.Serialize(integration.Config, _jsonOptions);

        var response = new UpsertIntegrationConfigRequest
        {
            Id = integration.Id,
            Provider = integration.Provider,
            SettingsJson = maskedJson,
            IsActive = integration.IsActive
        };

        await Send.OkAsync(response, ct);
    }
}
