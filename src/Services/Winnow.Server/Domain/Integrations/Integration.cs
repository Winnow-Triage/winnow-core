using Winnow.Integrations.Domain;
using Winnow.Server.Domain.Core;
using Winnow.Server.Domain.Integrations.Events;

namespace Winnow.Server.Domain.Integrations;

/// <summary>
/// Represents a configured third-party integration (e.g., GitHub, Jira, Trello) for a Project.
/// </summary>
public class Integration : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Provider { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ProjectId { get; private set; }

    public IntegrationConfig Config { get; private set; }
    public bool IsActive { get; private set; }
    public string? Token { get; private set; }

    // Private EF constructor
    private Integration()
    {
        Provider = null!;
        Config = null!;
    }

    /// <summary>
    /// Creates a new integration.
    /// </summary>
    public Integration(
        Guid organizationId,
        Guid projectId,
        string provider,
        IntegrationConfig config,
        string? token = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required.", nameof(organizationId));
        if (projectId == Guid.Empty)
            throw new ArgumentException("Project ID is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider name is required.", nameof(provider));
        ArgumentNullException.ThrowIfNull(config);

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ProjectId = projectId;
        Provider = provider;
        Config = config;
        Token = token;
        IsActive = true;
    }

    // ──────────────────────────────────────────────────────────────
    // Configuration
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the configuration using the polymorphic domain model (`IntegrationConfig.Merge`).
    /// </summary>
    public void UpdateConfig(IntegrationConfig newConfig)
    {
        ArgumentNullException.ThrowIfNull(newConfig);

        Config = Config.Merge(newConfig);
        _domainEvents.Add(new IntegrationConfigUpdatedEvent(Id, ProjectId, Provider));
    }

    /// <summary>
    /// Updates the integration token.
    /// </summary>
    public void UpdateToken(string? token)
    {
        Token = token;
    }

    // ──────────────────────────────────────────────────────────────
    // Activation state
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Deactivates the integration.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        _domainEvents.Add(new IntegrationDeactivatedEvent(Id, ProjectId, Provider));
    }

    /// <summary>
    /// Reactivates the integration.
    /// </summary>
    public void Reactivate()
    {
        if (IsActive) return;

        IsActive = true;
        _domainEvents.Add(new IntegrationReactivatedEvent(Id, ProjectId, Provider));
    }
}
