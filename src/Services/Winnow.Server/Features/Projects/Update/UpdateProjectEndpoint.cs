using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

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

public sealed class UpdateProjectEndpoint(WinnowDbContext dbContext) : ProjectScopedEndpoint<UpdateProjectRequest, UpdateProjectResponse>
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
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == req.CurrentProjectId && p.OwnerId == req.CurrentUserId && p.OrganizationId == req.CurrentOrganizationId, ct);

        if (project == null)
        {
            ThrowError("Project not found", 404);
            return;
        }

        // Update properties
        project.Rename(req.Name);
        project.ChangeTeam(req.TeamId);

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new UpdateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt
        }, ct);
    }
}
