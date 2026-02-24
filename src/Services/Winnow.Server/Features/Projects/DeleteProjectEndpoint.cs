using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public sealed class DeleteProjectEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext)
    : EndpointWithoutRequest
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
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
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        dbContext.Projects.Remove(project);

        await dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
