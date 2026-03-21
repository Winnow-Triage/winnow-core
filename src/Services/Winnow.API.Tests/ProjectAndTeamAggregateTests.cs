using Winnow.API.Domain.Projects;
using Winnow.API.Domain.Projects.Events;
using Winnow.API.Domain.Teams;
using Winnow.API.Domain.Teams.Events;

namespace Winnow.API.Tests;

public class ProjectAggregateTests
{
    private static readonly Guid SomeOrg = Guid.NewGuid();
    private const string SomeOwner = "user-abc";

    private static Project CreateProject(string? name = null) =>
        new(SomeOrg, name ?? "My Project", SomeOwner, "hash-primary-1");

    // ──────────────────────────────────────────────────────────────
    // Construction
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_RaisesProjectCreatedEvent()
    {
        var project = CreateProject();

        var evt = Assert.Single(project.DomainEvents.OfType<ProjectCreatedEvent>());
        Assert.Equal(project.Id, evt.ProjectId);
        Assert.Equal(SomeOrg, evt.OrganizationId);
        Assert.Equal(SomeOwner, evt.OwnerId);
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Project(SomeOrg, " ", SomeOwner, "hash"));
    }

    // ──────────────────────────────────────────────────────────────
    // API key rotation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RotateApiKey_SetsNewHashAndDemotesOldOne()
    {
        var project = CreateProject();
        var initialHash = project.ApiKeyHash;
        var newHash = "hash-new-1";
        var expiry = DateTimeOffset.UtcNow.AddDays(7);

        project.RotateApiKey(newHash, expiry);

        Assert.Equal(newHash, project.ApiKeyHash);
        Assert.Equal(initialHash, project.SecondaryApiKeyHash);
        Assert.Equal(expiry, project.SecondaryApiKeyExpiresAt);
        Assert.Single(project.DomainEvents.OfType<ProjectApiKeyRotatedEvent>());
    }

    [Fact]
    public void RotateApiKey_WithPastExpiry_Throws()
    {
        var project = CreateProject();

        Assert.Throws<ArgumentException>(() =>
            project.RotateApiKey("hash-new-1", DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void ForceSetPrimaryApiKey_NukesSecondaryKey()
    {
        var project = CreateProject();
        project.RotateApiKey("hash-2", DateTimeOffset.UtcNow.AddDays(7));
        Assert.NotNull(project.SecondaryApiKeyHash);

        project.ForceSetPrimaryApiKey("hash-3");

        Assert.Equal("hash-3", project.ApiKeyHash);
        Assert.Null(project.SecondaryApiKeyHash);
        Assert.Null(project.SecondaryApiKeyExpiresAt);
        Assert.Equal(2, project.DomainEvents.OfType<ProjectApiKeyRotatedEvent>().Count());
    }

    // ──────────────────────────────────────────────────────────────
    // Team membership
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChangeTeam_SetsTeamIdAndRaisesEvent()
    {
        var project = CreateProject();
        var teamId = Guid.NewGuid();
        project.ClearDomainEvents();

        project.ChangeTeam(teamId);

        Assert.Equal(teamId, project.TeamId);
        var evt = Assert.Single(project.DomainEvents.OfType<ProjectTeamAssignedEvent>());
        Assert.Equal(project.Id, evt.ProjectId);
        Assert.Equal(teamId, evt.TeamId);
    }

    [Fact]
    public void ChangeTeam_SameTeam_IsIdempotent()
    {
        var project = CreateProject();
        var teamId = Guid.NewGuid();
        project.ChangeTeam(teamId);
        project.ClearDomainEvents();

        project.ChangeTeam(teamId);

        Assert.Empty(project.DomainEvents);
    }

    [Fact]
    public void RemoveFromTeam_ClearsTeamId()
    {
        var project = CreateProject();
        project.ChangeTeam(Guid.NewGuid());

        project.RemoveFromTeam();

        Assert.Null(project.TeamId);
    }
}

public class TeamAggregateTests
{
    private static readonly Guid SomeOrg = Guid.NewGuid();

    private static Team CreateTeam(string? name = null) =>
        new(SomeOrg, name ?? "Backend Team");

    // ──────────────────────────────────────────────────────────────
    // Construction
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_RaisesTeamCreatedEvent()
    {
        var team = CreateTeam("Backend Team");

        var evt = Assert.Single(team.DomainEvents.OfType<TeamCreatedEvent>());
        Assert.Equal(team.Id, evt.TeamId);
        Assert.Equal("Backend Team", evt.Name);
        Assert.Equal(SomeOrg, evt.OrganizationId);
    }

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Team(SomeOrg, " "));
    }

    // ──────────────────────────────────────────────────────────────
    // Rename
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_ToDifferentName_RaisesEvent()
    {
        var team = CreateTeam("Old Name");
        team.ClearDomainEvents();

        team.Rename("New Name");

        Assert.Equal("New Name", team.Name);
        var evt = Assert.Single(team.DomainEvents.OfType<TeamRenamedEvent>());
        Assert.Equal(team.Id, evt.TeamId);
        Assert.Equal("Old Name", evt.OldName);
        Assert.Equal("New Name", evt.NewName);
    }

    [Fact]
    public void Rename_ToSameName_IsIdempotent()
    {
        var team = CreateTeam("Same Name");
        team.ClearDomainEvents();

        team.Rename("Same Name");

        Assert.Empty(team.DomainEvents);
    }

    [Fact]
    public void Rename_WithEmptyName_Throws()
    {
        var team = CreateTeam();
        Assert.Throws<ArgumentException>(() => team.Rename("  "));
    }
}
