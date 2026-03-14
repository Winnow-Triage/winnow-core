using Winnow.Server.Features.Projects.Dtos;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Projects.List;

public class ListProjectsRequest : OrganizationScopedRequest
{
    public bool OrgWide { get; set; }
}

public sealed class ListProjectsEndpoint(IMediator mediator)
    : OrganizationScopedEndpoint<ListProjectsRequest, List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
        Summary(s =>
        {
            s.Summary = "List projects";
            s.Description = "Retrieves a list of projects. If OrgWide is true and user is admin, returns all projects in the organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListProjectsRequest req, CancellationToken ct)
    {
        var query = new ListProjectsQuery
        {
            OrgWide = req.OrgWide,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        var result = await mediator.Send(query, ct);
        await Send.OkAsync(result, ct);
    }
}