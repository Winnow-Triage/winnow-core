using System.ComponentModel.DataAnnotations;

namespace Winnow.Server.Entities;

public class IntegrationConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Provider { get; set; } = string.Empty; // "GitHub", "Trello", "Jira"

    [Required]
    public string SettingsJson { get; set; } = "{}"; // Serialized provider-specific settings

    public bool IsActive { get; set; } = true;
}
