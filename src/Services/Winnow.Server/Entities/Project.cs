using System;

namespace Winnow.Server.Entities;

public class Project : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;

    public string? SecondaryApiKeyHash { get; set; }

    public DateTimeOffset? SecondaryApiKeyExpiresAt { get; set; }
    public Guid? TeamId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Team? Team { get; set; }
    public Organization? Organization { get; set; }
    public ApplicationUser? Owner { get; set; }
    public ICollection<Integration> Integrations { get; set; } = new List<Integration>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<Cluster> Clusters { get; set; } = new List<Cluster>();
}
