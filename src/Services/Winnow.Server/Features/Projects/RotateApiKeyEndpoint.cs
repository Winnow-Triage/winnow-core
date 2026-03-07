using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
// Make sure you are using the Domain namespace, not the old Entities one!
using Winnow.Server.Domain.Projects;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Projects;

public class RotateApiKeyRequest
{
    // FastEndpoints Magic: Automatically grabs this from the route!
    public Guid ProjectId { get; set; }
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
            s.Description = "Rotates the API Key for a specific project.";
            s.Response<RotateApiKeyResponse>(200, "API Key rotated successfully");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RotateApiKeyRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
            return;
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == req.ProjectId
                                   && p.OwnerId == userId
                                   && p.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (project == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Generate the new primary key (Infrastructure concern - stays in the handler!)
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(project.Id, "wm_live_");
        var newKeyHash = apiKeyService.HashKey(plaintextApiKey);

        project.RotateApiKey(newKeyHash, req.ExpiresAt);

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new RotateApiKeyResponse { ApiKey = plaintextApiKey }, ct);
    }
}