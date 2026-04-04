namespace Winnow.API.Features.Projects.Dtos;

/// <summary>
/// Project details.
/// </summary>
public record ProjectDto(
    Guid Id,
    string Name,
    string ApiKey,
    Guid? TeamId = null,
    Uri? DiscordWebhookUrl = null,
    bool HasSecondaryKey = false,
    DateTimeOffset? SecondaryApiKeyExpiresAt = null);
