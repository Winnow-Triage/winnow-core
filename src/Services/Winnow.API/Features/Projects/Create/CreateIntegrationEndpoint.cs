using MediatR;
using Winnow.API.Domain.Integrations;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Projects.Create;

public class CreateIntegrationRequest : ProjectScopedRequest
{
    public Guid ProjectId { get; set; }

    public Guid? Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string SettingsJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
}

public sealed class CreateIntegrationEndpoint(IMediator mediator)
    : ProjectScopedEndpoint<CreateIntegrationRequest, Integration>
{
    public override void Configure()
    {
        Post("/integrations");
        Summary(s =>
        {
            s.Summary = "Create project integration";
            s.Description = "Creates a new integration configuration scoped to a project. Provider settings must be valid JSON.";
            s.Response<Integration>(200, "Integration saved successfully");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CreateIntegrationRequest req, CancellationToken ct)
    {
        var command = new CreateIntegrationCommand
        {
            ProjectId = req.ProjectId,
            Id = req.Id,
            Provider = req.Provider,
            SettingsJson = req.SettingsJson,
            IsActive = req.IsActive,
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
