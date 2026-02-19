using Winnow.Integrations.Domain;
using System.Text.Json;

namespace Winnow.Server.Tests;

public class IntegrationConfigTests
{
    [Fact]
    public void Merge_GitHubConfig_WhenApiKeyMasked_PreservesOriginal()
    {
        // Arrange
        var original = new GitHubConfig
        {
            ApiKey = "original-secret-key",
            Owner = "test-owner",
            Repo = "test-repo"
        };
        
        var incoming = new GitHubConfig
        {
            ApiKey = "******", // Masked secret
            Owner = "new-owner", // Changed
            Repo = "new-repo" // Changed
        };
        
        // Act
        var merged = original.Merge(incoming) as GitHubConfig;
        
        // Assert
        Assert.NotNull(merged);
        Assert.Equal("original-secret-key", merged.ApiKey); // Preserved via MergeSecret
        Assert.Equal("new-owner", merged.Owner); // Updated
        Assert.Equal("new-repo", merged.Repo); // Updated
    }
    
    [Fact]
    public void Merge_GitHubConfig_WhenApiKeyNotMasked_UpdatesValue()
    {
        // Arrange
        var original = new GitHubConfig
        {
            ApiKey = "original-secret-key",
            Owner = "test-owner",
            Repo = "test-repo"
        };
        
        var incoming = new GitHubConfig
        {
            ApiKey = "new-secret-key", // New secret
            Owner = "new-owner",
            Repo = "new-repo"
        };
        
        // Act
        var merged = original.Merge(incoming) as GitHubConfig;
        
        // Assert
        Assert.NotNull(merged);
        Assert.Equal("new-secret-key", merged.ApiKey); // Updated
        Assert.Equal("new-owner", merged.Owner);
        Assert.Equal("new-repo", merged.Repo);
    }
    
    [Fact]
    public void Merge_TrelloConfig_WhenSecretsMasked_PreservesOriginals()
    {
        // Arrange
        var original = new TrelloConfig
        {
            ApiKey = "original-api-key",
            Token = "original-token",
            ListId = "original-list"
        };
        
        var incoming = new TrelloConfig
        {
            ApiKey = "******", // Masked
            Token = "******", // Masked
            ListId = "new-list" // Changed
        };
        
        // Act
        var merged = original.Merge(incoming) as TrelloConfig;
        
        // Assert
        Assert.NotNull(merged);
        Assert.Equal("original-api-key", merged.ApiKey); // Preserved via MergeSecret
        Assert.Equal("original-token", merged.Token); // Preserved via MergeSecret
        Assert.Equal("new-list", merged.ListId); // Updated
    }
    
    [Fact]
    public void Merge_JiraConfig_WhenApiTokenMasked_PreservesOriginal()
    {
        // Arrange
        var original = new JiraConfig
        {
            BaseUrl = new Uri("https://original.atlassian.net"),
            UserEmail = "original@example.com",
            ApiToken = "original-token",
            ProjectKey = "ORIG"
        };
        
        var incoming = new JiraConfig
        {
            BaseUrl = new Uri("https://new.atlassian.net"), // Changed
            UserEmail = "new@example.com", // Changed
            ApiToken = "******", // Masked
            ProjectKey = "NEW" // Changed
        };
        
        // Act
        var merged = original.Merge(incoming) as JiraConfig;
        
        // Assert
        Assert.NotNull(merged);
        Assert.Equal(new Uri("https://new.atlassian.net"), merged.BaseUrl); // Updated
        Assert.Equal("new@example.com", merged.UserEmail); // Updated
        Assert.Equal("original-token", merged.ApiToken); // Preserved via MergeSecret
        Assert.Equal("NEW", merged.ProjectKey); // Updated
    }
    
    [Fact]
    public void Merge_WithDifferentConfigType_ThrowsArgumentException()
    {
        // Arrange
        IntegrationConfig original = new GitHubConfig();
        IntegrationConfig incoming = new TrelloConfig();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => original.Merge(incoming));
        Assert.Contains("Cannot merge TrelloConfig with GitHubConfig", exception.Message);
    }
    
    [Fact]
    public void Merge_WithNullIncomingConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IntegrationConfig original = new GitHubConfig();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => original.Merge(null!));
    }
    
    [Fact]
    public void MergeSecret_HelperMethod_WorksCorrectly()
    {
        // Arrange
        var current = "current-secret";
        var masked = "******";
        var newValue = "new-secret";
        
        // Act & Assert
        Assert.Equal(current, IntegrationConfig.MergeSecret(current, masked)); // Preserve current when masked
        Assert.Equal(newValue, IntegrationConfig.MergeSecret(current, newValue)); // Use new value when not masked
        Assert.Equal("", IntegrationConfig.MergeSecret(current, "")); // Empty string is not masked
        Assert.Equal(current, IntegrationConfig.MergeSecret(current, "******")); // Only exact "******" is treated as masked
    }
    
    [Fact]
    public void Properties_ReturnCorrectValues()
    {
        // Arrange
        var github = new GitHubConfig
        {
            ApiKey = "test-key",
            Owner = "test-owner",
            Repo = "test-repo"
        };
        
        // Act & Assert
        Assert.Equal("test-key", github.ApiKey);
        Assert.Equal("test-owner", github.Owner);
        Assert.Equal("test-repo", github.Repo);
    }
    
    [Fact]
    public void JsonSerialization_WithPolymorphism_Works()
    {
        // Arrange
        IntegrationConfig config = new GitHubConfig
        {
            ApiKey = "secret",
            Owner = "owner",
            Repo = "repo"
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        // Act
        var json = JsonSerializer.Serialize(config, options);
        var deserialized = JsonSerializer.Deserialize<IntegrationConfig>(json, options);
        
        // Assert
        Assert.NotNull(json);
        Assert.Contains("$type", json); // Should have discriminator
        Assert.NotNull(deserialized);
        Assert.IsType<GitHubConfig>(deserialized);
        
        var github = (GitHubConfig)deserialized;
        Assert.Equal("secret", github.ApiKey);
        Assert.Equal("owner", github.Owner);
        Assert.Equal("repo", github.Repo);
    }
}