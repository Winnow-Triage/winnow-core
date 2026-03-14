namespace Winnow.Server.Features.Projects.Dtos;

/// <summary>
/// Project details.
/// </summary>
public record ProjectDto(
    Guid Id,
    string Name,
    string ApiKey,
    Guid? TeamId = null,
    bool HasSecondaryKey = false,
    DateTimeOffset? SecondaryApiKeyExpiresAt = null);
