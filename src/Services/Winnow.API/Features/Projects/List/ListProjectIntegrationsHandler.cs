using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Projects.List;

[RequirePermission("projects:read")]
public record ListProjectIntegrationsQuery : IRequest<List<ProjectIntegrationDto>>, IProjectScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ListProjectIntegrationsHandler(WinnowDbContext db)
    : IRequestHandler<ListProjectIntegrationsQuery, List<ProjectIntegrationDto>>
{
    public async Task<List<ProjectIntegrationDto>> Handle(ListProjectIntegrationsQuery request, CancellationToken ct)
    {
        var integrations = await db.Integrations
            .AsNoTracking()
            .Where(i => i.ProjectId == request.ProjectId)
            .Select(i => new { i.Id, i.Provider, i.Name, i.IsActive })
            .ToListAsync(ct);

        var dtos = integrations.Select(i => new ProjectIntegrationDto(i.Id, i.Provider, i.Name, i.IsActive)).ToList();

        return dtos;
    }
}
