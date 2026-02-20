using Microsoft.AspNetCore.Identity;

namespace Winnow.Server.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationship: A user can be a member/owner of multiple projects
    public ICollection<Project> Projects { get; } = new List<Project>();

    // Relationship: A user can be a member of multiple organizations
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
}
