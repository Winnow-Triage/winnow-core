using Winnow.API.Features.Projects.Dtos;
using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Features.Projects.Create;

[RequirePermission("projects:manage")]
public record CreateProjectCommand : IRequest<ProjectDto>, IOrganizationScopedRequest
{
    public string Name { get; set; } = default!;
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CreateProjectHandler(
    WinnowDbContext dbContext,
    IApiKeyService apiKeyService) : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        // 1. We must generate the Project ID early so we can embed it in the key
        var projectId = Guid.NewGuid();

        // 2. Generate the plaintext key (embeds the ProjectId within it)
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(projectId, "wm_live_");

        // 3. Hash the entire plaintext key for the database
        var apiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        var project = new Domain.Projects.Project(
            request.CurrentOrganizationId,
            request.Name,
            request.CurrentUserId,
            apiKeyHash,
            projectId
        );

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(ct);

        // 4. Return the PLAINTEXT key exactly once to the frontend
        return new ProjectDto(project.Id, project.Name, plaintextApiKey);
    }
}
