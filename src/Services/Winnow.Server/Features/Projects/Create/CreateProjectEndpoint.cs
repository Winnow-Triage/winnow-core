using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects.Create;

/// <summary>
/// Project details.
/// </summary>
/// <param name="Id">Unique identifier of the project.</param>
/// <param name="Name">Name of the project.</param>
/// <param name="ApiKey">API Key key for the project.</param>
/// <param name="TeamId">The ID of the team this project belongs to, if any.</param>
/// <param name="HasSecondaryKey">Indicates if there is an active secondary (old) key.</param>
/// <param name="SecondaryApiKeyExpiresAt">When the secondary key will expire, if any.</param>
public record ProjectDto(
    Guid Id,
    string Name,
    string ApiKey,
    Guid? TeamId = null,
    bool HasSecondaryKey = false,
    DateTimeOffset? SecondaryApiKeyExpiresAt = null);


/// <summary>
/// Request to create a new project.
/// </summary>
public class CreateProjectRequest : OrganizationScopedRequest
{
    /// <summary>
    /// Name of the new project.
    /// </summary>
    public string Name { get; set; } = default!;
}

public sealed class CreateProjectEndpoint(
    WinnowDbContext dbContext,
    Infrastructure.Security.IApiKeyService apiKeyService) : OrganizationScopedEndpoint<CreateProjectRequest, ProjectDto>
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
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        // 1. We must generate the Project ID early so we can embed it in the key
        var projectId = Guid.NewGuid();

        // 2. Generate the plaintext key (embeds the ProjectId within it)
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(projectId, "wm_live_");

        // 3. Hash the entire plaintext key for the database
        var apiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        var project = new Domain.Projects.Project(
            req.CurrentOrganizationId,
            req.Name,
            req.CurrentUserId,
            apiKeyHash,
            projectId
        );

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        // 4. Return the PLAINTEXT key exactly once to the frontend
        await Send.OkAsync(new ProjectDto(project.Id, project.Name, plaintextApiKey), ct);
    }
}
