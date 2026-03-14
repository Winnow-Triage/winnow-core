using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Features.Projects.RegenerateApiKey;

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
    public async Task<RegenerateApiKeyResponse> Handle(RegenerateApiKeyCommand request, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId
                                   && p.OwnerId == request.CurrentUserId
                                   && p.OrganizationId == request.CurrentOrganizationId, ct);

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
        await dbContext.SaveChangesAsync(ct);

        return new RegenerateApiKeyResponse { ApiKey = plaintextApiKey };
    }
}
