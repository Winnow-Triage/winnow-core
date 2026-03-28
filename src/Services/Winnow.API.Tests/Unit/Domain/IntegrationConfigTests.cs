using Winnow.Integrations.Domain;
using Xunit;

namespace Winnow.API.Tests.Unit.Domain;

public class IntegrationConfigTests
{
    [Fact]
    public void Merge_IncompatibleTypes_ThrowsArgumentException()
    {
        // Scenario 1: GitHubConfig -> JiraConfig
        var github = new GitHubConfig
        {
            ApiKey = "github-key",
            Owner = "owner",
            Repo = "repo"
        };

        var jira = new JiraConfig
        {
            BaseUrl = new Uri("https://example.atlassian.net"),
            UserEmail = "user@example.com",
            ApiToken = "jira-token",
            ProjectKey = "PROJ"
        };

        var ex1 = Assert.Throws<ArgumentException>(() => github.Merge(jira));
        Assert.Contains($"Cannot merge {nameof(JiraConfig)} with {nameof(GitHubConfig)}", ex1.Message);

        // Scenario 2: JiraConfig -> TrelloConfig
        var trello = new TrelloConfig
        {
            ApiKey = "trello-key",
            Token = "trello-token",
            ListId = "list123"
        };

        var ex2 = Assert.Throws<ArgumentException>(() => jira.Merge(trello));
        Assert.Contains($"Cannot merge {nameof(TrelloConfig)} with {nameof(JiraConfig)}", ex2.Message);

        // Scenario 3: TrelloConfig -> GitHubConfig
        var ex3 = Assert.Throws<ArgumentException>(() => trello.Merge(github));
        Assert.Contains($"Cannot merge {nameof(GitHubConfig)} with {nameof(TrelloConfig)}", ex3.Message);
    }
}