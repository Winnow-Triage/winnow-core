using FastEndpoints;
using Winnow.Integrations.Domain;

using Winnow.Server.Domain.Integrations;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public class CreateIntegrationRequest : ProjectScopedRequest
{
    public Guid ProjectId { get; set; }

    public Guid? Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string SettingsJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
}

public sealed class CreateIntegrationEndpoint(
    WinnowDbContext db,
    IEnumerable<IIntegrationConfigDeserializationStrategy> deserializationStrategies)
    : ProjectScopedEndpoint<CreateIntegrationRequest, Integration>
{
    public override void Configure()
    {
        Post("/integrations");
        Summary(s =>
        {
            s.Summary = "Create project integration";
            s.Description = "Creates a new integration configuration scoped to a project. Provider settings must be valid JSON.";
            s.Response<Integration>(200, "Integration saved successfully");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CreateIntegrationRequest req, CancellationToken ct)
    {
        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(req.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {req.Provider}");

        IntegrationConfig newConfig = strategy.Deserialize(req.SettingsJson);

        Integration? integration;

        if (req.Id.HasValue)
        {
            integration = await db.Integrations.FindAsync([req.Id.Value], ct);
            if (integration == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            if (integration.ProjectId != req.ProjectId)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }

            integration.UpdateConfig(newConfig);
            if (req.IsActive) integration.Reactivate(); else integration.Deactivate();
        }
        else
        {
            integration = new Integration(
                req.CurrentOrganizationId,
                req.ProjectId,
                req.Provider,
                newConfig
            );
            if (!req.IsActive) integration.Deactivate();

            await db.Integrations.AddAsync(integration, ct);
        }

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(integration, ct);
    }
}
