using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

/// <summary>
/// Request DTO for updating a project.
/// </summary>
public class UpdateProjectRequest
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

public sealed class UpdateProjectEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext) : Endpoint<UpdateProjectRequest, UpdateProjectResponse>
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Project name cannot be empty.", 400);
            return;
        }

        var projectIdStr = Route<string>("ProjectId");
        if (!Guid.TryParse(projectIdStr, out var projectId))
        {
            ThrowError("Invalid Project ID", 400);
            return;
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId && p.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (project == null)
        {
            ThrowError("Project not found", 404);
            return;
        }

        // Update properties
        project.Name = req.Name.Trim();
        project.TeamId = req.TeamId;

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new UpdateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt
        }, ct);
    }
}
