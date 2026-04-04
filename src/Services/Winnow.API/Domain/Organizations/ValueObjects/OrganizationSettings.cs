using System;
using System.Text.Json.Serialization;
using Winnow.API.Domain.Common;

namespace Winnow.API.Domain.Organizations.ValueObjects;

public class OrganizationSettings
{
    public Guid OrganizationId { get; private set; }
    public bool ToxicityFilterEnabled { get; private set; }
    public ToxicityThresholds ToxicityLimits { get; private set; }
    public AIConfiguration AIConfig { get; private set; }
    public NotificationSettings Notifications { get; private set; }

    /// <summary>
    /// Parameterized constructor for JSON deserialization and internal creation.
    /// This ensures System.Text.Json can populate private-set properties.
    /// </summary>
    [JsonConstructor]
    public OrganizationSettings(
        Guid organizationId,
        bool toxicityFilterEnabled,
        ToxicityThresholds toxicityLimits,
        AIConfiguration aiConfig,
        NotificationSettings notifications)
    {
        OrganizationId = organizationId;
        ToxicityFilterEnabled = toxicityFilterEnabled;
        ToxicityLimits = toxicityLimits ?? ToxicityThresholds.Default;
        AIConfig = aiConfig ?? AIConfiguration.Default;
        Notifications = notifications ?? NotificationSettings.CreateDefault();
    }

    public static OrganizationSettings Create(Guid organizationId)
    {
        return new OrganizationSettings(
            organizationId,
            true,
            ToxicityThresholds.Default,
            AIConfiguration.Default,
            NotificationSettings.CreateDefault()
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

    public void UpdateNotificationThresholds(NotificationSettings settings)
    {
        Notifications = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void UpdateAIConfiguration(AIConfiguration newConfig)
    {
        AIConfig = newConfig;
    }
}