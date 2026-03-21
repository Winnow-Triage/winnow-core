using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Features.Projects.RotateApiKey;

[RequirePermission("projects:manage")]
public record RotateApiKeyCommand : IRequest<RotateApiKeyResponse>, IOrganizationScopedRequest
{
    public Guid ProjectId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class RotateApiKeyHandler(
    WinnowDbContext dbContext,
    IApiKeyService apiKeyService) : IRequestHandler<RotateApiKeyCommand, RotateApiKeyResponse>
{
    public async Task<RotateApiKeyResponse> Handle(RotateApiKeyCommand request, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId
                                   && p.OwnerId == request.CurrentUserId
                                   && p.OrganizationId == request.CurrentOrganizationId, ct);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Generate the new primary key
        var plaintextApiKey = apiKeyService.GeneratePlaintextKey(project.Id, "wm_live_");
        var newKeyHash = apiKeyService.HashKey(plaintextApiKey);

        project.RotateApiKey(newKeyHash, request.ExpiresAt);

        await dbContext.SaveChangesAsync(ct);

        return new RotateApiKeyResponse { ApiKey = plaintextApiKey };
    }
}
