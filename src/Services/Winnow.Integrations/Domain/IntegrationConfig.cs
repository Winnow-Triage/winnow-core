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
[JsonDerivedType(typeof(DiscordConfig), typeDiscriminator: "discord")]
[JsonDerivedType(typeof(SlackConfig), typeDiscriminator: "slack")]
[JsonDerivedType(typeof(TeamsConfig), typeDiscriminator: "teams")]
[JsonDerivedType(typeof(EmailConfig), typeDiscriminator: "email")]
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

    /// <summary>
    /// Helper method to merge URI fields.
    /// </summary>
    public static Uri? MergeUri(Uri? current, Uri? incoming)
    {
        // If incoming is null, it might mean "don't change" or "clear"
        // But for our UI, we'll assume it's a replacement if not null.
        return incoming ?? current;
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
            BaseUrl = MergeUri(BaseUrl, other.BaseUrl) ?? BaseUrl,
            UserEmail = other.UserEmail,
            ApiToken = MergeSecret(ApiToken, other.ApiToken),
            ProjectKey = other.ProjectKey
        };
    }
}

/// <summary>
/// Discord integration configuration domain model.
/// </summary>
public record DiscordConfig : IntegrationConfig
{
    public Uri? WebhookUrl { get; init; }

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not DiscordConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(DiscordConfig)}");

        return this with
        {
            WebhookUrl = MergeUri(WebhookUrl, other.WebhookUrl)
        };
    }
}

/// <summary>
/// Slack integration configuration domain model.
/// </summary>
public record SlackConfig : IntegrationConfig
{
    public Uri? WebhookUrl { get; init; }

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not SlackConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(SlackConfig)}");

        return this with
        {
            WebhookUrl = MergeUri(WebhookUrl, other.WebhookUrl)
        };
    }
}

/// <summary>
/// MS Teams integration configuration domain model.
/// </summary>
public record TeamsConfig : IntegrationConfig
{
    public Uri? WebhookUrl { get; init; }

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        ValidateIncoming(incoming);

        if (incoming is not TeamsConfig other)
            throw new ArgumentException($"Cannot merge {incoming.GetType().Name} with {nameof(TeamsConfig)}");

        return this with
        {
            WebhookUrl = MergeUri(WebhookUrl, other.WebhookUrl)
        };
    }
}

public record EmailConfig : IntegrationConfig
{
    public string RecipientEmail { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public string? VerificationToken { get; init; }

    public override IntegrationConfig Merge(IntegrationConfig incoming)
    {
        if (incoming is not EmailConfig emailConfig) return this;

        // If email changed, drop verification status
        if (!string.IsNullOrWhiteSpace(emailConfig.RecipientEmail) && emailConfig.RecipientEmail != RecipientEmail)
        {
            return this with
            {
                RecipientEmail = emailConfig.RecipientEmail,
                IsVerified = false,
                VerificationToken = null
            };
        }

        return this;
    }
}
