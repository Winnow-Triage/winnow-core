using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Projects.RevokeSecondaryApiKey;

[RequirePermission("projects:manage")]
public record RevokeSecondaryApiKeyCommand : IRequest, IOrganizationScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class RevokeSecondaryApiKeyHandler(WinnowDbContext dbContext) : IRequestHandler<RevokeSecondaryApiKeyCommand>
{
    public async Task Handle(RevokeSecondaryApiKeyCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId
                                   && p.OwnerId == request.CurrentUserId
                                   && p.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        project.RevokeSecondaryApiKey();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
