namespace Winnow.Server.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public string SubscriptionTier { get; set; } = "Free";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuspended { get; set; }

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();

    // Helper methods
    public bool IsPaidTier()
    {
        return SubscriptionTier != "Free";
    }

    public bool CanCreateMultipleProjects()
    {
        return IsPaidTier() || SubscriptionTier == "Free" && Teams.SelectMany(t => t.Projects).Count() < 3;
    }
}