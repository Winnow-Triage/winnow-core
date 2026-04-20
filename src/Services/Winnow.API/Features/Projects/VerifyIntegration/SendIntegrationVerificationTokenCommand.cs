using Wolverine;
using Microsoft.Extensions.Logging;
using Winnow.API.Services.Emails;

namespace Winnow.API.Features.Projects.VerifyIntegration;

public record SendIntegrationVerificationTokenCommand
{
    public string RecipientEmail { get; init; } = string.Empty;
    public Guid IntegrationId { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

public class SendIntegrationVerificationTokenCommandHandler(
    IEmailService emailService,
    Microsoft.Extensions.Configuration.IConfiguration config,
    ILogger<SendIntegrationVerificationTokenCommandHandler> logger)
{
    public async Task Handle(SendIntegrationVerificationTokenCommand command, CancellationToken ct)
    {
        var appUrl = config["AppUrl"] ?? throw new InvalidOperationException("AppUrl configuration is missing.");
        var verifyUrl = new Uri($"{appUrl.TrimEnd('/')}/projects/{command.ProjectId}/settings?verifyIntegration={command.IntegrationId}&token={command.Token}");

        await emailService.SendIntegrationVerificationAsync(command.RecipientEmail, command.ProjectName, verifyUrl);

        logger.LogInformation("Sent integration verification email to {Email} for integration {IntegrationId}", command.RecipientEmail, command.IntegrationId);
    }
}
