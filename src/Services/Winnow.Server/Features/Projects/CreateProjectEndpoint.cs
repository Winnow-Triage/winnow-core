using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
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

public sealed class CreateProjectEndpoint(
    WinnowDbContext dbContext,
    ITenantContext tenantContext,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService) : Endpoint<CreateProjectRequest, ProjectDto>
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
        Options(x => x.RequireAuthorization());
        // Authorized users only
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Validate user has access to the current organization
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
        }

        // Verify user is a member of this organization
        var organizationMember = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.UserId == userId && om.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (organizationMember == null)
        {
            ThrowError("User does not have access to this organization", 403);
        }

        // 1. We must generate the Project ID early so we can embed it in the key
        var projectId = Guid.NewGuid();

        // 2. Generate the plaintext key (embeds the ProjectId within it)
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(projectId, "wm_live_");

        // 3. Hash the entire plaintext key for the database
        var apiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        var project = new Project
        {
            Id = projectId, // Assign the ID we generated
            Name = req.Name,
            OwnerId = userId,
            OrganizationId = tenantContext.CurrentOrganizationId.Value,
            ApiKeyHash = apiKeyHash, // Store ONLY the hash
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        // 4. Return the PLAINTEXT key exactly once to the frontend
        await Send.OkAsync(new ProjectDto(project.Id, project.Name, plaintextApiKey), ct);
    }
}
