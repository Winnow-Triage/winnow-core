using System.ComponentModel.DataAnnotations;
using Winnow.Integrations.Domain;

namespace Winnow.Server.Entities;

public class Integration : ITenantEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Provider { get; set; } = string.Empty; // "GitHub", "Trello", "Jira"

    public Guid OrganizationId { get; set; } // Tenant isolation

    public IntegrationConfig Config { get; private set; } = null!; // Polymorphic domain model

    public bool IsActive { get; set; } = true;

    // Added as a hypothetical property for encryption-at-rest demonstration
    public string? Token { get; set; }

    /// <summary>
    /// Updates the configuration using the polymorphic domain model.
    /// </summary>
    /// <param name="newConfig">The new configuration to apply</param>
    public void UpdateConfig(IntegrationConfig newConfig)
    {
        ArgumentNullException.ThrowIfNull(newConfig);

        Config = Config == null ? newConfig : Config.Merge(newConfig);
    }
}
