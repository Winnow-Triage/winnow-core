using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Projects.List;

public class ListProjectIntegrationsRequest : ProjectScopedRequest
{
    public Guid ProjectId { get; set; }
}

public record ProjectIntegrationDto(Guid Id, string Provider, string Name, bool IsActive);

public sealed class ListProjectIntegrationsEndpoint(IMediator mediator)
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
        var query = new ListProjectIntegrationsQuery
        {
            ProjectId = req.ProjectId,
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        var result = await mediator.Send(query, ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}
