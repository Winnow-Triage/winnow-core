namespace Winnow.Server.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty; // Reference to ApplicationUser.Id
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = "Member"; // Admin, Member, etc.
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsLocked { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public Organization? Organization { get; set; }

    // Helper methods
    public bool IsAdmin()
    {
        return string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Role, "owner", StringComparison.OrdinalIgnoreCase);
    }

    public bool HasAccessToOrganization(Guid organizationId)
    {
        return OrganizationId == organizationId;
    }
}