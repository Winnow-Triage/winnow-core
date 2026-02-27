using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public sealed class RevokeSecondaryApiKeyEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/revoke-secondary");
        Summary(s =>
        {
            s.Summary = "Revoke Secondary API Key";
            s.Description = "Immediately invalidates the secondary (old) API key for a project.";
            s.Response(204, "Secondary key revoked");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
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

        project.SecondaryApiKeyHash = null;
        project.SecondaryApiKeyExpiresAt = null;

        await dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
