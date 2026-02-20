namespace Winnow.Server.Entities;

public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization? Organization { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    
    // Helper methods
    public bool BelongsToOrganization(Guid organizationId)
    {
        return OrganizationId == organizationId;
    }
}