using System.Text.Json.Serialization;

namespace Winnow.API.Domain.Organizations.ValueObjects;

public class OrganizationSettings
{
    public Guid OrganizationId { get; private set; }
    public bool ToxicityFilterEnabled { get; private set; }
    public ToxicityThresholds ToxicityLimits { get; private set; }
    public AIConfiguration AIConfig { get; private set; }

    /// <summary>
    /// Parameterized constructor for JSON deserialization and internal creation.
    /// This ensures System.Text.Json can populate private-set properties.
    /// </summary>
    [JsonConstructor]
    public OrganizationSettings(
        Guid organizationId,
        bool toxicityFilterEnabled,
        ToxicityThresholds toxicityLimits,
        AIConfiguration aiConfig)
    {
        OrganizationId = organizationId;
        ToxicityFilterEnabled = toxicityFilterEnabled;
        ToxicityLimits = toxicityLimits ?? ToxicityThresholds.Default;
        AIConfig = aiConfig ?? AIConfiguration.Default;
    }

    public static OrganizationSettings Create(Guid organizationId)
    {
        return new OrganizationSettings(
            organizationId,
            true,
            ToxicityThresholds.Default,
            AIConfiguration.Default
        );
    }

    public void UpdateToxicityLimits(ToxicityThresholds newLimits)
    {
        ToxicityLimits = newLimits;
    }

    public void ToggleToxicityFilter(bool enabled)
    {
        ToxicityFilterEnabled = enabled;
    }

    public void UpdateAIConfiguration(AIConfiguration newConfig)
    {
        AIConfig = newConfig;
    }
}