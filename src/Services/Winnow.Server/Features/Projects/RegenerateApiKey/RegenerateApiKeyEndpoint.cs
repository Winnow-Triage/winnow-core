using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Projects.RegenerateApiKey;

public class RegenerateApiKeyRequest : OrganizationScopedRequest
{
    public Guid ProjectId { get; set; }
}

/// <summary>
/// Response containing the new plaintext API Key.
/// </summary>
public class RegenerateApiKeyResponse
{
    /// <summary>
    /// The new plaintext API key (only returned exactly once).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class RegenerateApiKeyEndpoint(IMediator mediator) : OrganizationScopedEndpoint<RegenerateApiKeyRequest, RegenerateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/api-key/regenerate");
        Summary(s =>
        {
            s.Summary = "Regenerate API Key";
            s.Description = "Regenerates the API Key for a specific project. The new plaintext key is returned exactly once.";
            s.Response<RegenerateApiKeyResponse>(200, "API Key regenerated successfully");
            s.Response(401, "Unauthorized");
            s.Response(404, "Project not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(RegenerateApiKeyRequest req, CancellationToken ct)
    {
        var command = new RegenerateApiKeyCommand
        {
            ProjectId = req.ProjectId,
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
