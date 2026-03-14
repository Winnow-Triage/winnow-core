using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Projects.Update;

/// <summary>
/// Request DTO for updating a project.
/// </summary>
public class UpdateProjectRequest : ProjectScopedRequest
{
    /// <summary>
    /// The new name for the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the team to assign this project to.
    /// </summary>
    public Guid? TeamId { get; set; }
}

/// <summary>
/// Response DTO containing the updated project details.
/// </summary>
public class UpdateProjectResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class UpdateProjectEndpoint(IMediator mediator) : ProjectScopedEndpoint<UpdateProjectRequest, UpdateProjectResponse>
{
    public override void Configure()
    {
        Put("/projects/{ProjectId}");
        Summary(s =>
        {
            s.Summary = "Update a Project";
            s.Description = "Updates the configuration of an existing project, such as renaming it.";
            s.Response<UpdateProjectResponse>(200, "Project updated successfully");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(UpdateProjectRequest req, CancellationToken ct)
    {
        var command = new UpdateProjectCommand
        {
            Name = req.Name,
            TeamId = req.TeamId,
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
