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
    public async Task<Integration> Handle(CreateIntegrationCommand request, CancellationToken cancellationToken)
    {
        var strategy = deserializationStrategies.FirstOrDefault(s => s.CanHandle(request.Provider))
            ?? throw new ArgumentException($"Unsupported provider: {request.Provider}");

        var config = strategy.Deserialize(request.SettingsJson);
        var (finalConfig, verificationToken) = PrepareConfigWithVerification(config, request.Id.HasValue ? await GetExistingRecipientEmail(request.Id.Value) : null);

        Integration integration;
        if (request.Id.HasValue)
        {
            integration = await UpdateExistingIntegrationAsync(request, finalConfig, cancellationToken);
        }
        else
        {
            integration = await CreateNewIntegrationAsync(request, finalConfig, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (verificationToken != null && finalConfig is EmailConfig emailConfig)
        {
            await NotifyVerificationRequiredAsync(integration, emailConfig, verificationToken, cancellationToken);
        }

        return integration;
    }

    private async Task<string?> GetExistingRecipientEmail(Guid id)
    {
        var integration = await db.Integrations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return integration?.Config is EmailConfig ec ? ec.RecipientEmail : null;
    }

    private static (IntegrationConfig Config, string? Token) PrepareConfigWithVerification(IntegrationConfig newConfig, string? existingEmail)
    {
        if (newConfig is EmailConfig newEmailConfig &&
            !string.IsNullOrWhiteSpace(newEmailConfig.RecipientEmail) &&
            existingEmail != newEmailConfig.RecipientEmail)
        {
            var token = Guid.NewGuid().ToString("N")[..8];
            return (newEmailConfig with { IsVerified = false, VerificationToken = token }, token);
        }
        return (newConfig, null);
    }

    private async Task<Integration> UpdateExistingIntegrationAsync(CreateIntegrationCommand request, IntegrationConfig config, CancellationToken ct)
    {
        var integration = await db.Integrations.FindAsync([request.Id!.Value], ct)
            ?? throw new InvalidOperationException("Integration not found.");

        if (integration.ProjectId != request.ProjectId)
            throw new UnauthorizedAccessException("Access denied.");

        integration.UpdateConfig(config);
        if (request.IsActive) integration.Reactivate(); else integration.Deactivate();
        integration.UpdateNotificationState(request.NotificationsEnabled);
        integration.UpdateName(request.Name);

        return integration;
    }

    private async Task<Integration> CreateNewIntegrationAsync(CreateIntegrationCommand request, IntegrationConfig config, CancellationToken ct)
    {
        var integration = new Integration(
            request.CurrentOrganizationId,
            request.ProjectId,
            request.Provider,
            request.Name,
            config
        );

        if (!request.IsActive) integration.Deactivate();
        integration.UpdateNotificationState(request.NotificationsEnabled);

        await db.Integrations.AddAsync(integration, ct);
        return integration;
    }

    private async Task NotifyVerificationRequiredAsync(Integration integration, EmailConfig config, string token, CancellationToken ct)
    {
        var projectName = await db.Projects
            .Where(p => p.Id == integration.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct) ?? "Your Project";

        await messageBus.PublishAsync(new SendIntegrationVerificationTokenCommand
        {
            IntegrationId = integration.Id,
            ProjectId = integration.ProjectId,
            ProjectName = projectName,
            RecipientEmail = config.RecipientEmail,
            Token = token
        });
    }
}
