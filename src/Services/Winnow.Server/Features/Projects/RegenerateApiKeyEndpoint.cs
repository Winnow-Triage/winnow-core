using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Projects;

/// <summary>
/// Response containing the new plaintext API Key.
/// </summary>
public class RegenerateApiKeyResponse
{
    /// <summary>
    /// The new plaintext API key (only returned exactly once).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class RegenerateApiKeyEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext,
    IApiKeyService apiKeyService) : Endpoint<EmptyRequest, RegenerateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/regenerate");
        Summary(s =>
        {
            s.Summary = "Regenerate API Key";
            s.Description = "Regenerates the API Key for a specific project. The new plaintext key is returned exactly once.";
            s.Response<RegenerateApiKeyResponse>(200, "API Key regenerated successfully");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
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

        // Generate the new plaintext key
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(project.Id, "wm_live_");

        // Hash the new plaintext key for the database
        var apiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        // Update the project
        project.ForceSetPrimaryApiKey(apiKeyHash);
        await dbContext.SaveChangesAsync(ct);

        // Return the PLAINTEXT key exactly once
        await Send.OkAsync(new RegenerateApiKeyResponse { ApiKey = plaintextApiKey }, ct);
    }
}
