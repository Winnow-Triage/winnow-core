using MediatR;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.Integrations.Domain;
using Winnow.API.Domain.Integrations;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Integrations.Strategies;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Projects.VerifyIntegration;

namespace Winnow.API.Features.Projects.Create;

[RequirePermission("projects:manage")]
public record CreateIntegrationCommand : IRequest<Integration>, IProjectScopedRequest
{
    public Guid ProjectId { get; set; }
    public Guid? Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public bool NotificationsEnabled { get; set; } = true;
    public Guid CurrentProjectId { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CreateIntegrationHandler(
    WinnowDbContext db,
    IMessageBus messageBus,
    IEnumerable<IIntegrationConfigDeserializationStrategy> deserializationStrategies)
    : IRequestHandler<CreateIntegrationCommand, Integration>
{
    public async Task<Integration> Handle(CreateIntegrationCommand request, CancellationToken ct)
    {
        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(request.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {request.Provider}");

        IntegrationConfig newConfig = strategy.Deserialize(request.SettingsJson);
        bool requiresEmailVerification = false;
        string? generatedVerificationToken = null;

        var projectName = await db.Projects
            .Where(p => p.Id == request.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct) ?? "Your Project";

        Integration? integration;

        if (request.Id.HasValue)
        {
            integration = await db.Integrations.FindAsync([request.Id.Value], ct);
            if (integration == null)
            {
                throw new InvalidOperationException("Integration not found.");
            }
            if (integration.ProjectId != request.ProjectId)
            {
                throw new UnauthorizedAccessException("Access denied.");
            }

            if (newConfig is EmailConfig newEmailConfig && !string.IsNullOrWhiteSpace(newEmailConfig.RecipientEmail))
            {
                if (integration.Config is not EmailConfig oldEmailConfig || oldEmailConfig.RecipientEmail != newEmailConfig.RecipientEmail)
                {
                    generatedVerificationToken = Guid.NewGuid().ToString("N");
                    newConfig = newEmailConfig with { IsVerified = false, VerificationToken = generatedVerificationToken };
                    requiresEmailVerification = true;
                }
            }

            integration.UpdateConfig(newConfig);
            if (request.IsActive) integration.Reactivate(); else integration.Deactivate();
            integration.UpdateNotificationState(request.NotificationsEnabled);
            integration.UpdateName(request.Name);
        }
        else
        {
            if (newConfig is EmailConfig newEmailConfig && !string.IsNullOrWhiteSpace(newEmailConfig.RecipientEmail))
            {
                generatedVerificationToken = Guid.NewGuid().ToString("N");
                newConfig = newEmailConfig with { IsVerified = false, VerificationToken = generatedVerificationToken };
                requiresEmailVerification = true;
            }

            integration = new Integration(
                request.CurrentOrganizationId,
                request.ProjectId,
                request.Provider,
                request.Name,
                newConfig
            );
            if (!request.IsActive) integration.Deactivate();
            integration.UpdateNotificationState(request.NotificationsEnabled);

            await db.Integrations.AddAsync(integration, ct);
        }

        await db.SaveChangesAsync(ct);

        if (requiresEmailVerification && newConfig is EmailConfig finalEmailConfig && generatedVerificationToken != null)
        {
            await messageBus.PublishAsync(new SendIntegrationVerificationTokenCommand
            {
                IntegrationId = integration.Id,
                ProjectId = integration.ProjectId,
                ProjectName = projectName,
                RecipientEmail = finalEmailConfig.RecipientEmail,
                Token = generatedVerificationToken
            });
        }

        return integration;
    }
}
