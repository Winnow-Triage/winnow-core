using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

/// <summary>
/// Request to create a new project.
/// </summary>
public class CreateProjectRequest
{
    /// <summary>
    /// Name of the new project.
    /// </summary>
    public string Name { get; set; } = default!;
}

public sealed class CreateProjectEndpoint(WinnowDbContext dbContext) : Endpoint<CreateProjectRequest, ProjectDto>
{
    public override void Configure()
    {
        Post("/projects");
        Summary(s =>
        {
            s.Summary = "Create a new project";
            s.Description = "Creates a new project for the authenticated user and generates an API key.";
            s.Response<ProjectDto>(200, "Project created successfully");
            s.Response(401, "Unauthorized");
        });
        // Authorized users only
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

        // Generate API Key (simple implementation for now)
        var apiKey = $"wm_live_{Guid.NewGuid().ToString("N")[..20]}";

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            OwnerId = userId,
            ApiKey = apiKey,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new ProjectDto(project.Id, project.Name, project.ApiKey), ct);
    }
}
