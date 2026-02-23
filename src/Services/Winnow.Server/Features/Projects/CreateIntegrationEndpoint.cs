using FastEndpoints;
using Winnow.Integrations.Domain;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public class CreateIntegrationRequest
{
    public Guid ProjectId { get; set; }

    public Guid? Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string SettingsJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
}

public sealed class CreateIntegrationEndpoint(
    WinnowDbContext db,
    ITenantContext tenantContext,
    IEnumerable<IIntegrationConfigDeserializationStrategy> deserializationStrategies)
    : Endpoint<CreateIntegrationRequest, Integration>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/integrations");
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
        // Global query filter ensures we only see projects for the current tenant
        var projectExists = db.Projects.Any(p => p.Id == req.ProjectId);
        if (!projectExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
        }
        else
        {
            integration = new Integration
            {
                ProjectId = req.ProjectId,
                OrganizationId = tenantContext.CurrentOrganizationId.GetValueOrDefault()
            };
            await db.Integrations.AddAsync(integration, ct);
        }

        integration.Provider = req.Provider;
        integration.IsActive = req.IsActive;

        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(req.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {req.Provider}");

        IntegrationConfig newConfig = strategy.Deserialize(req.SettingsJson);
        integration.UpdateConfig(newConfig);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(integration, ct);
    }
}
