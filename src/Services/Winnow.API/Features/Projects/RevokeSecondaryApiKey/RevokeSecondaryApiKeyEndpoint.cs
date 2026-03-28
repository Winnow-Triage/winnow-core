using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Projects.RevokeSecondaryApiKey;

public class RevokeSecondaryApiKeyRequest : OrganizationScopedRequest
{
    public Guid ProjectId { get; set; }
}

public sealed class RevokeSecondaryApiKeyEndpoint(IMediator mediator) : OrganizationScopedEndpoint<RevokeSecondaryApiKeyRequest>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/revoke-secondary");
        Summary(s =>
        {
            s.Summary = "Revoke Secondary API Key";
            s.Description = "Immediately invalidates the secondary (old) API key for a project.";
            s.Response(204, "Secondary key revoked");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RevokeSecondaryApiKeyRequest req, CancellationToken ct)
    {
        var command = new RevokeSecondaryApiKeyCommand
        {
            ProjectId = req.ProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
