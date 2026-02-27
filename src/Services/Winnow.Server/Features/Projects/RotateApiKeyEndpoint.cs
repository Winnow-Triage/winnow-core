using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Projects;

public class RotateApiKeyRequest
{
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class RotateApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class RotateApiKeyEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext,
    IApiKeyService apiKeyService) : Endpoint<RotateApiKeyRequest, RotateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/rotate");
        Summary(s =>
        {
            s.Summary = "Rotate API Key";
            s.Description = "Rotates the API Key for a specific project. The old key remains valid as a secondary key until the specified Expiration Date (ExpiresAt). If null, it remains valid until manually revoked.";
            s.Response<RotateApiKeyResponse>(200, "API Key rotated successfully");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RotateApiKeyRequest req, CancellationToken ct)
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

        // Move current key to secondary
        project.SecondaryApiKeyHash = project.ApiKeyHash;
        project.SecondaryApiKeyExpiresAt = req.ExpiresAt;

        // Generate the new primary key
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(project.Id, "wm_live_");
        project.ApiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new RotateApiKeyResponse { ApiKey = plaintextApiKey }, ct);
    }
}
