namespace Winnow.API.Infrastructure.Configuration;

internal class IntegrationSettings
{
    public GitHubSettings GitHub { get; set; } = new();
    public TrelloSettings Trello { get; set; } = new();
    public JiraSettings Jira { get; set; } = new();
}

internal class GitHubSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
}

internal class TrelloSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ListId { get; set; } = string.Empty;
}

internal class JiraSettings
{
    public Uri BaseUrl { get; set; } = new Uri("https://localhost");
    public string UserEmail { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}
