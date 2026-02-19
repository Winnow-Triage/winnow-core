using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public record ProjectDto(Guid Id, string Name, string ApiKey);

public sealed class ListProjectsEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
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
