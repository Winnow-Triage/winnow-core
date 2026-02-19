using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

/// <summary>
/// Project details.
/// </summary>
/// <param name="Id">Unique identifier of the project.</param>
/// <param name="Name">Name of the project.</param>
/// <param name="ApiKey">API Key key for the project.</param>
public record ProjectDto(Guid Id, string Name, string ApiKey);

public sealed class ListProjectsEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
        Summary(s =>
        {
            s.Summary = "List user projects";
            s.Description = "Retrieves a list of all projects owned by the authenticated user.";
            s.Response<List<ProjectDto>>(200, "List of projects");
            s.Response(401, "Unauthorized");
        });
        // We will enable strict auth in Program.cs, but we can also enforce it here
        // Claims: assuming standard JWT claims
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == userId)
            .Select(p => new ProjectDto(p.Id, p.Name, p.ApiKey))
            .ToListAsync(ct);

        await Send.OkAsync(projects, ct);
    }
}
