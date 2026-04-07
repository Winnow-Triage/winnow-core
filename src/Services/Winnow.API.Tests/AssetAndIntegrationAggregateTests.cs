using Winnow.Integrations.Domain;
using Winnow.API.Domain.Assets;
using Winnow.API.Domain.Assets.Events;
using Winnow.API.Domain.Assets.ValueObjects;
using Winnow.API.Domain.Integrations;
using Winnow.API.Domain.Integrations.Events;

namespace Winnow.API.Tests;

public class AssetAggregateTests
{
    private static readonly Guid SomeOrg = Guid.NewGuid();
    private static readonly Guid SomeProject = Guid.NewGuid();
    private static readonly Guid SomeReport = Guid.NewGuid();

    private static Asset CreateAsset() =>
        new(SomeOrg, SomeProject, SomeReport, "screenshot.png", "raw/screenshot.png", 1024, "image/png");

    [Fact]
    public void Constructor_SetsStatusToPending()
    {
        var asset = CreateAsset();

        Assert.Equal(AssetStatus.Pending, asset.Status);
        Assert.Null(asset.ScannedAt);
        Assert.Null(asset.CleanS3Key);
    }

    [Fact]
    public void Constructor_WithInvalidSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Asset(SomeOrg, SomeProject, SomeReport, "screenshot.png", "s3key", 0)); // Size 0
    }

    // ──────────────────────────────────────────────────────────────
    // Scanning Lifecycle
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsClean_FromPending_RaisesEventAndUpdatesStatus()
    {
        var asset = CreateAsset();
        var cleanKey = "clean/screenshot.png";

        asset.MarkAsClean(cleanKey);

        Assert.Equal(AssetStatus.Clean, asset.Status);
        Assert.Equal(cleanKey, asset.CleanS3Key);
        Assert.NotNull(asset.ScannedAt);

        var evt = Assert.Single(asset.DomainEvents.OfType<AssetScanPassedEvent>());
        Assert.Equal(cleanKey, evt.CleanS3Key);
    }

    [Fact]
    public void MarkAsInfected_FromPending_RaisesEventAndUpdatesStatus()
    {
        var asset = CreateAsset();

        asset.MarkAsInfected();

        Assert.Equal(AssetStatus.Infected, asset.Status);
        Assert.Null(asset.CleanS3Key);
        Assert.NotNull(asset.ScannedAt);

        Assert.Single(asset.DomainEvents.OfType<AssetScanVirusDetectedEvent>());
    }

    [Fact]
    public void MarkAsFailed_FromPending_RaisesEventAndUpdatesStatus()
    {
        var asset = CreateAsset();

        asset.MarkAsFailed("ClamAV connection timed out");

        Assert.Equal(AssetStatus.Failed, asset.Status);
        Assert.Null(asset.ScannedAt);

        var evt = Assert.Single(asset.DomainEvents.OfType<AssetScanFailedEvent>());
        Assert.Equal("ClamAV connection timed out", evt.ErrorMessage);
    }

    [Fact]
    public void MarkAsClean_WhenAlreadyClean_ThrowsInvalidOperation()
    {
        var asset = CreateAsset();
        asset.MarkAsClean("clean-key");

        Assert.Throws<InvalidOperationException>(() => asset.MarkAsClean("another-key"));
    }
}

public class IntegrationAggregateTests
{
    private static readonly Guid SomeOrg = Guid.NewGuid();
    private static readonly Guid SomeProject = Guid.NewGuid();

    private static Winnow.API.Domain.Integrations.Integration CreateIntegration() =>
        new(SomeOrg, SomeProject, "GitHub", "My Integration", new GitHubConfig { ApiKey = "initial" });

    [Fact]
    public void Constructor_SetsIsActiveToTrue()
    {
        var integration = CreateIntegration();

        Assert.True(integration.IsActive);
        Assert.NotNull(integration.Config);
    }

    // ──────────────────────────────────────────────────────────────
    // Configuration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateConfig_MergesConfigAndRaisesEvent()
    {
        var integration = CreateIntegration();
        var newConfig = new GitHubConfig { ApiKey = "new-key" };

        integration.UpdateConfig(newConfig);

        var config = Assert.IsType<GitHubConfig>(integration.Config);
        Assert.Equal("new-key", config.ApiKey);
        var evt = Assert.Single(integration.DomainEvents.OfType<IntegrationConfigUpdatedEvent>());
        Assert.Equal("GitHub", evt.Provider);
    }

    // ──────────────────────────────────────────────────────────────
    // Activation state
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsInactiveAndRaisesEvent()
    {
        var integration = CreateIntegration();

        integration.Deactivate();

        Assert.False(integration.IsActive);
        Assert.Single(integration.DomainEvents.OfType<IntegrationDeactivatedEvent>());
    }

    [Fact]
    public void Reactivate_WhenInactive_SetsActiveAndRaisesEvent()
    {
        var integration = CreateIntegration();
        integration.Deactivate();
        integration.ClearDomainEvents();

        integration.Reactivate();

        Assert.True(integration.IsActive);
        Assert.Single(integration.DomainEvents.OfType<IntegrationReactivatedEvent>());
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var integration = CreateIntegration();
        integration.Deactivate();
        integration.ClearDomainEvents();

        integration.Deactivate();

        Assert.Empty(integration.DomainEvents);
    }
}
