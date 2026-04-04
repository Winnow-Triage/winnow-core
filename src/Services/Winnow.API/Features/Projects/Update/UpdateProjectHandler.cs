using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Domain.Projects;

namespace Winnow.API.Features.Projects.Update;

[RequirePermission("projects:manage")]
public record UpdateProjectCommand : IRequest<UpdateProjectResponse>, IProjectScopedRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? TeamId { get; set; }
    public Uri? DiscordWebhookUrl { get; set; }
    public Guid ProjectId { get; set; }
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
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.OrganizationId == request.CurrentOrganizationId, ct);

        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Update properties
        project.Rename(request.Name);
        project.ChangeTeam(request.TeamId);
        project.UpdateDiscordWebhook(request.DiscordWebhookUrl);

        await dbContext.SaveChangesAsync(ct);

        return new UpdateProjectResponse
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt
        };
    }
}
