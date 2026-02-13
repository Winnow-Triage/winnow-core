namespace Winnow.Server.Infrastructure.Configuration;

public class IntegrationSettings
{
    public GitHubSettings GitHub { get; set; } = new();
    public TrelloSettings Trello { get; set; } = new();
    public JiraSettings Jira { get; set; } = new();
}

public class GitHubSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
}

public class TrelloSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ListId { get; set; } = string.Empty;
}

public class JiraSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}
