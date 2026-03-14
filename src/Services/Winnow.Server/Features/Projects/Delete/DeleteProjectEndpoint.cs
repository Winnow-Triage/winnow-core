using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Projects.Delete;

public class DeleteProjectRequest : ProjectScopedRequest { }

public sealed class DeleteProjectEndpoint(IMediator mediator)
    : ProjectScopedEndpoint<DeleteProjectRequest>
{
    public override void Configure()
    {
        Delete("/projects/{ProjectId}");
        Summary(s =>
        {
            s.Summary = "Delete Project";
            s.Description = "Permanently deletes a project, including all of its error reports and generated API keys.";
            s.Response(204, "Project deleted successfully");
            s.Response(400, "Invalid Request");
            s.Response(404, "Project Not Found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DeleteProjectRequest req, CancellationToken ct)
    {
        var command = new DeleteProjectCommand
        {
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(cancellation: ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
