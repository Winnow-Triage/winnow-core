using Winnow.API.Infrastructure.Security.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Integrations;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Projects.Get;

[RequirePermission("projects:read")]
public record GetIntegrationQuery : IRequest<Integration>, IProjectScopedRequest
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class GetIntegrationRequest : ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class GetIntegrationEndpoint(IMediator mediator)
    : ProjectScopedEndpoint<GetIntegrationRequest, Integration>
{
    public override void Configure()
    {
        Get("/projects/{ProjectId}/integrations/{Id}");
        Summary(s =>
        {
            s.Summary = "Get project integration details";
            s.Description = "Retrieves the configuration and state of a specific integration.";
            s.Response<Integration>(200, "Integration details retrieved successfully");
            s.Response(404, "Integration not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetIntegrationRequest req, CancellationToken ct)
    {
        var query = new GetIntegrationQuery
        {
            Id = req.Id,
            ProjectId = req.ProjectId,
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            var result = await mediator.Send(query, ct);
            await Send.OkAsync(result, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
