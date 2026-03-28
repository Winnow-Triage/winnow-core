using System.Text.Json.Serialization;

namespace Winnow.Integrations.Domain;

/// <summary>
/// Abstract base record for polymorphic integration configuration.
/// Represents the domain model for integration settings.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GitHubConfig), typeDiscriminator: "github")]
[JsonDerivedType(typeof(TrelloConfig), typeDiscriminator: "trello")]
[JsonDerivedType(typeof(JiraConfig), typeDiscriminator: "jira")]
public abstract record IntegrationConfig
{
    /// <summary>
    /// Merges incoming configuration with current configuration.
    /// </summary>
    /// <param name="incoming">The incoming configuration to merge</param>
    /// <returns>New merged configuration</returns>
    public abstract IntegrationConfig Merge(IntegrationConfig incoming);

    /// <summary>
    /// Validates that incoming configuration is not null.
    /// </summary>
    /// <param name="incoming">The incoming configuration</param>
    /// <exception cref="ArgumentNullException">Thrown when incoming is null</exception>
    protected static void ValidateIncoming(IntegrationConfig incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
    }

    /// <summary>
    /// Helper method to merge secret fields, preserving masked values.
    /// </summary>
    /// <param name="current">Current secret value</param>
    /// <param name="incoming">Incoming secret value</param>
    /// <returns>Merged secret value</returns>
    public static string MergeSecret(string current, string incoming)
    {
        return incoming == "******" ? current : incoming;
    }
}

/// <summary>
/// GitHub integration configuration domain model.
/// </summary>
public record GitHubConfig : IntegrationConfig
{
    public string ApiKey { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public string Repo { get; init; } = string.Empty;

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not GitHubConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(GitHubConfig)}");

        return this with
        {
            ApiKey = MergeSecret(ApiKey, other.ApiKey),
            Owner = other.Owner,
            Repo = other.Repo
        };
    }
}

/// <summary>
/// Trello integration configuration domain model.
/// </summary>
public record TrelloConfig : IntegrationConfig
{
    public string ApiKey { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string ListId { get; init; } = string.Empty;

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not TrelloConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(TrelloConfig)}");

        return this with
        {
            ApiKey = MergeSecret(ApiKey, other.ApiKey),
            Token = MergeSecret(Token, other.Token),
            ListId = other.ListId
        };
    }
}

/// <summary>
/// Jira integration configuration domain model.
/// </summary>
public record JiraConfig : IntegrationConfig
{
    public Uri BaseUrl { get; init; } = new("https://localhost");
    public string UserEmail { get; init; } = string.Empty;
    public string ApiToken { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not JiraConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(JiraConfig)}");

        return this with
        {
            BaseUrl = other.BaseUrl,
            UserEmail = other.UserEmail,
            ApiToken = MergeSecret(ApiToken, other.ApiToken),
            ProjectKey = other.ProjectKey
        };
    }
}
