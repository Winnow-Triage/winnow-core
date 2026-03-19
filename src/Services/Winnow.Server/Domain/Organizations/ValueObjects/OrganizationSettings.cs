using System.Text.Json.Serialization;

namespace Winnow.Server.Domain.Organizations.ValueObjects;

public class OrganizationSettings
{
    public Guid OrganizationId { get; private set; }
    public bool ToxicityFilterEnabled { get; private set; }
    public ToxicityThresholds ToxicityLimits { get; private set; }

    // Private constructor required by EF Core and JSON deserialization
    [JsonConstructor]
    private OrganizationSettings()
    {
        ToxicityLimits = ToxicityThresholds.Default;
    }

    public static OrganizationSettings Create(Guid organizationId)
    {
        return new OrganizationSettings
        {
            OrganizationId = organizationId,
            ToxicityFilterEnabled = true,
            ToxicityLimits = ToxicityThresholds.Default
        };
    }

    public void UpdateToxicityLimits(ToxicityThresholds newLimits)
    {
        ToxicityLimits = newLimits;
    }
}