using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects.Update;

public record UpdateProjectCommand : IRequest<UpdateProjectResponse>, IProjectScopedRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? TeamId { get; set; }
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class UpdateProjectHandler(WinnowDbContext dbContext) : IRequestHandler<UpdateProjectCommand, UpdateProjectResponse>
{
    public async Task<UpdateProjectResponse> Handle(UpdateProjectCommand request, CancellationToken ct)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.CurrentProjectId && p.OwnerId == request.CurrentUserId && p.OrganizationId == request.CurrentOrganizationId, ct);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Update properties
        project.Rename(request.Name);
        project.ChangeTeam(request.TeamId);

        await dbContext.SaveChangesAsync(ct);

        return new UpdateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt
        };
    }
}
