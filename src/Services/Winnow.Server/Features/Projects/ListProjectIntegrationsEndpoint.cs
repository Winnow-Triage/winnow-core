using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public class ListProjectIntegrationsRequest : ProjectScopedRequest
{
    public Guid ProjectId { get; set; }
}

public record ProjectIntegrationDto(Guid Id, string Provider, string Name, bool IsActive);

public sealed class ListProjectIntegrationsEndpoint(WinnowDbContext db)
    : ProjectScopedEndpoint<ListProjectIntegrationsRequest, List<ProjectIntegrationDto>>
{
    public override void Configure()
    {
        Get("/integrations");
        Summary(s =>
        {
            s.Summary = "List project integrations";
            s.Description = "Retrieves a list of all integrations configured for a specific project.";
            s.Response<List<ProjectIntegrationDto>>(200, "List of integrations");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListProjectIntegrationsRequest req, CancellationToken ct)
    {
        var integrations = await db.Integrations
            .AsNoTracking()
            .Where(i => i.ProjectId == req.ProjectId)
            .Select(i => new { i.Id, i.Provider, i.IsActive })
            .ToListAsync(ct);

        var dtos = integrations.Select(i => new ProjectIntegrationDto(i.Id, i.Provider, $"{i.Provider} Integration", i.IsActive)).ToList();

        await Send.OkAsync(dtos, cancellation: ct);
    }
}
