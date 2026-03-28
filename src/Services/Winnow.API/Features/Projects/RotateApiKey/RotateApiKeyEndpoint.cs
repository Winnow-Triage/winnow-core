using MediatR;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Projects.RotateApiKey;

public class RotateApiKeyRequest : OrganizationScopedRequest
{
    // FastEndpoints Magic: Automatically grabs this from the route!
    public Guid ProjectId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class RotateApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class RotateApiKeyEndpoint(IMediator mediator) : OrganizationScopedEndpoint<RotateApiKeyRequest, RotateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/rotate");
        Summary(s =>
        {
            s.Summary = "Rotate API Key";
            s.Description = "Rotates the API Key for a specific project.";
            s.Response<RotateApiKeyResponse>(200, "API Key rotated successfully");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RotateApiKeyRequest req, CancellationToken ct)
    {
        var command = new RotateApiKeyCommand
        {
            ProjectId = req.ProjectId,
            ExpiresAt = req.ExpiresAt,
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
    }
}