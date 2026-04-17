using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;
using Winnow.Integrations.Domain;

namespace Winnow.API.Features.Projects.VerifyIntegration;

public class VerifyIntegrationEmailRequest : ProjectScopedRequest
{
    public Guid IntegrationId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public sealed class VerifyIntegrationEmailEndpoint(IMediator mediator)
    : ProjectScopedEndpoint<VerifyIntegrationEmailRequest, object>
{
    public override void Configure()
    {
        Post("/projects/{ProjectId}/integrations/{IntegrationId}/verify");
        Summary(s =>
        {
            s.Summary = "Verify an email integration";
            s.Description = "Verifies the email destination for a specific integration using a token.";
            s.Response(200, "Verification successful");
            s.Response(400, "Invalid token or not an email integration");
            s.Response(404, "Integration not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(VerifyIntegrationEmailRequest req, CancellationToken ct)
    {
        var command = new VerifyIntegrationEmailCommand
        {
            ProjectId = req.ProjectId,
            IntegrationId = req.IntegrationId,
            Token = req.Token,
            CurrentProjectId = req.CurrentProjectId,
            CurrentOrganizationId = req.CurrentOrganizationId,
            CurrentUserId = req.CurrentUserId,
            CurrentUserRoles = req.CurrentUserRoles
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.OkAsync(new object(), cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message);
        }
    }
}

[RequirePermission("projects:manage")]
public class VerifyIntegrationEmailCommand : IRequest, IProjectScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid IntegrationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class VerifyIntegrationEmailHandler(WinnowDbContext db) : IRequestHandler<VerifyIntegrationEmailCommand>
{
    public async Task Handle(VerifyIntegrationEmailCommand request, CancellationToken ct)
    {
        var integration = await db.Integrations.FindAsync([request.IntegrationId], ct)
            ?? throw new InvalidOperationException("Integration not found.");

        if (integration.ProjectId != request.ProjectId)
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        if (integration.Config is not EmailConfig emailConfig)
        {
            throw new InvalidOperationException("Integration is not an Email configuration.");
        }

        if (emailConfig.IsVerified)
        {
            return; // Already verified
        }

        if (string.IsNullOrWhiteSpace(emailConfig.VerificationToken) || emailConfig.VerificationToken != request.Token)
        {
            throw new InvalidOperationException("Invalid or missing verification token.");
        }

        // Verify it
        var newConfig = emailConfig with { IsVerified = true, VerificationToken = null };
        integration.UpdateConfig(newConfig);

        await db.SaveChangesAsync(ct);
    }
}
