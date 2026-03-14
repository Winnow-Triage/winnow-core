using MediatR;
using Winnow.Integrations.Domain;
using Winnow.Server.Domain.Integrations;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Integrations.Strategies;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects.Create;

public record CreateIntegrationCommand : IRequest<Integration>, IProjectScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid? Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CreateIntegrationHandler(
    WinnowDbContext db,
    IEnumerable<IIntegrationConfigDeserializationStrategy> deserializationStrategies)
    : IRequestHandler<CreateIntegrationCommand, Integration>
{
    public async Task<Integration> Handle(CreateIntegrationCommand request, CancellationToken ct)
    {
        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(request.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {request.Provider}");

        IntegrationConfig newConfig = strategy.Deserialize(request.SettingsJson);

        Integration? integration;

        if (request.Id.HasValue)
        {
            integration = await db.Integrations.FindAsync([request.Id.Value], ct);
            if (integration == null)
            {
                throw new InvalidOperationException("Integration not found.");
            }
            if (integration.ProjectId != request.ProjectId)
            {
                throw new UnauthorizedAccessException("Access denied.");
            }

            integration.UpdateConfig(newConfig);
            if (request.IsActive) integration.Reactivate(); else integration.Deactivate();
        }
        else
        {
            integration = new Integration(
                request.CurrentOrganizationId,
                request.ProjectId,
                request.Provider,
                newConfig
            );
            if (!request.IsActive) integration.Deactivate();

            await db.Integrations.AddAsync(integration, ct);
        }

        await db.SaveChangesAsync(ct);

        return integration;
    }
}
