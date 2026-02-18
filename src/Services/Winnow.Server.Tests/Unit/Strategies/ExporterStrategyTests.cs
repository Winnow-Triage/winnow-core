using Moq;
using Winnow.Integrations;
using Winnow.Integrations.Domain;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Integrations.Strategies;

namespace Winnow.Server.Tests.Unit.Strategies;

public class ExporterStrategyTests
{
    [Fact]
    public void GitHubExporterCreationStrategy_CanHandle_ReturnsTrue_ForGitHubConfig()
    {
        // Arrange
        var strategy = new GitHubExporterCreationStrategy();
        var config = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };

        // Act
        var result = strategy.CanHandle(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GitHubExporterCreationStrategy_CanHandle_ReturnsFalse_ForOtherConfigTypes()
    {
        // Arrange
        var strategy = new GitHubExporterCreationStrategy();
        var jiraConfig = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };
        var trelloConfig = new TrelloConfig { ApiKey = "test", Token = "test", ListId = "test" };

        // Act
        var resultForJira = strategy.CanHandle(jiraConfig);
        var resultForTrello = strategy.CanHandle(trelloConfig);

        // Assert
        Assert.False(resultForJira);
        Assert.False(resultForTrello);
    }

    [Fact]
    public void GitHubExporterCreationStrategy_Create_ReturnsGitHubExporter()
    {
        // Arrange
        var strategy = new GitHubExporterCreationStrategy();
        var config = new GitHubConfig { ApiKey = "test-key", Owner = "test-owner", Repo = "test-repo" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act
        var result = strategy.Create(config, httpClient);

        // Assert
        Assert.IsType<GitHubExporter>(result);
    }

    [Fact]
    public void GitHubExporterCreationStrategy_Create_ThrowsArgumentException_ForWrongConfigType()
    {
        // Arrange
        var strategy = new GitHubExporterCreationStrategy();
        var wrongConfig = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => strategy.Create(wrongConfig, httpClient));
        Assert.Contains("Expected GitHubConfig", exception.Message);
    }

    [Fact]
    public void JiraExporterCreationStrategy_CanHandle_ReturnsTrue_ForJiraConfig()
    {
        // Arrange
        var strategy = new JiraExporterCreationStrategy();
        var config = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };

        // Act
        var result = strategy.CanHandle(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void JiraExporterCreationStrategy_CanHandle_ReturnsFalse_ForOtherConfigTypes()
    {
        // Arrange
        var strategy = new JiraExporterCreationStrategy();
        var githubConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var trelloConfig = new TrelloConfig { ApiKey = "test", Token = "test", ListId = "test" };

        // Act
        var resultForGitHub = strategy.CanHandle(githubConfig);
        var resultForTrello = strategy.CanHandle(trelloConfig);

        // Assert
        Assert.False(resultForGitHub);
        Assert.False(resultForTrello);
    }

    [Fact]
    public void JiraExporterCreationStrategy_Create_ReturnsJiraExporter()
    {
        // Arrange
        var strategy = new JiraExporterCreationStrategy();
        var config = new JiraConfig { BaseUrl = "https://test.atlassian.net", UserEmail = "user@test.com", ApiToken = "test-token", ProjectKey = "PROJ" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act
        var result = strategy.Create(config, httpClient);

        // Assert
        Assert.IsType<JiraExporter>(result);
    }

    [Fact]
    public void JiraExporterCreationStrategy_Create_ThrowsArgumentException_ForWrongConfigType()
    {
        // Arrange
        var strategy = new JiraExporterCreationStrategy();
        var wrongConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => strategy.Create(wrongConfig, httpClient));
        Assert.Contains("Expected JiraConfig", exception.Message);
    }

    [Fact]
    public void TrelloExporterCreationStrategy_CanHandle_ReturnsTrue_ForTrelloConfig()
    {
        // Arrange
        var strategy = new TrelloExporterCreationStrategy();
        var config = new TrelloConfig { ApiKey = "test", Token = "test", ListId = "test" };

        // Act
        var result = strategy.CanHandle(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TrelloExporterCreationStrategy_CanHandle_ReturnsFalse_ForOtherConfigTypes()
    {
        // Arrange
        var strategy = new TrelloExporterCreationStrategy();
        var githubConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var jiraConfig = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };

        // Act
        var resultForGitHub = strategy.CanHandle(githubConfig);
        var resultForJira = strategy.CanHandle(jiraConfig);

        // Assert
        Assert.False(resultForGitHub);
        Assert.False(resultForJira);
    }

    [Fact]
    public void TrelloExporterCreationStrategy_Create_ReturnsTrelloExporter()
    {
        // Arrange
        var strategy = new TrelloExporterCreationStrategy();
        var config = new TrelloConfig { ApiKey = "test-api-key", Token = "test-token", ListId = "test-list-id" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act
        var result = strategy.Create(config, httpClient);

        // Assert
        Assert.IsType<TrelloExporter>(result);
    }

    [Fact]
    public void TrelloExporterCreationStrategy_Create_ThrowsArgumentException_ForWrongConfigType()
    {
        // Arrange
        var strategy = new TrelloExporterCreationStrategy();
        var wrongConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => strategy.Create(wrongConfig, httpClient));
        Assert.Contains("Expected TrelloConfig", exception.Message);
    }

    [Fact]
    public void DefaultExporterCreationStrategy_CanHandle_ReturnsTrue_ForAnyConfig()
    {
        // Arrange
        var strategy = new DefaultExporterCreationStrategy();
        var githubConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var jiraConfig = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };
        var trelloConfig = new TrelloConfig { ApiKey = "test", Token = "test", ListId = "test" };

        // Act
        var resultForGitHub = strategy.CanHandle(githubConfig);
        var resultForJira = strategy.CanHandle(jiraConfig);
        var resultForTrello = strategy.CanHandle(trelloConfig);

        // Assert
        Assert.True(resultForGitHub);
        Assert.True(resultForJira);
        Assert.True(resultForTrello);
    }

    [Fact]
    public void DefaultExporterCreationStrategy_Create_ReturnsNullExporter_ForAnyConfig()
    {
        // Arrange
        var strategy = new DefaultExporterCreationStrategy();
        var githubConfig = new GitHubConfig { ApiKey = "test", Owner = "owner", Repo = "repo" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act
        var result = strategy.Create(githubConfig, httpClient);

        // Assert
        Assert.IsType<NullExporter>(result);
    }

    [Fact]
    public void DefaultExporterCreationStrategy_Create_ReturnsNullExporter_ForOtherConfigTypes()
    {
        // Arrange
        var strategy = new DefaultExporterCreationStrategy();
        var jiraConfig = new JiraConfig { BaseUrl = "test", UserEmail = "test", ApiToken = "test", ProjectKey = "test" };
        var trelloConfig = new TrelloConfig { ApiKey = "test", Token = "test", ListId = "test" };
        var httpClient = new Mock<HttpClient>().Object;

        // Act
        var resultForJira = strategy.Create(jiraConfig, httpClient);
        var resultForTrello = strategy.Create(trelloConfig, httpClient);

        // Assert
        Assert.IsType<NullExporter>(resultForJira);
        Assert.IsType<NullExporter>(resultForTrello);
    }
}
