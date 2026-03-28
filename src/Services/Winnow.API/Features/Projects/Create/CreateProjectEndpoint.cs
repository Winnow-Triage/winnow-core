using Winnow.API.Features.Projects.Dtos;
using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Projects.Create;

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

public sealed class CreateProjectEndpoint(IMediator mediator) : OrganizationScopedEndpoint<CreateProjectRequest, ProjectDto>
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
        var command = new CreateProjectCommand
        {
            Name = req.Name,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        var result = await mediator.Send(command, ct);
        await Send.OkAsync(result, ct);
    }
}
