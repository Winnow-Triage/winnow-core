using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Features.Projects.RegenerateApiKey;

[RequirePermission("projects:manage")]
public record RegenerateApiKeyCommand : IRequest<RegenerateApiKeyResponse>, IOrganizationScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class RegenerateApiKeyHandler(
    WinnowDbContext dbContext,
    IApiKeyService apiKeyService) : IRequestHandler<RegenerateApiKeyCommand, RegenerateApiKeyResponse>
{
    public async Task<RegenerateApiKeyResponse> Handle(RegenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId
                                   && p.OwnerId == request.CurrentUserId
                                   && p.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Generate the new plaintext key
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(project.Id, "wm_live_");

        // Hash the new plaintext key for the database
        var apiKeyHash = apiKeyService.HashKey(plaintextApiKey);

        // Update the project
        project.ForceSetPrimaryApiKey(apiKeyHash);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegenerateApiKeyResponse { ApiKey = plaintextApiKey };
    }
}
