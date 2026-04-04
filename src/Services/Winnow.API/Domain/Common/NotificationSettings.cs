using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Common;

/// <summary>
/// A value object representing common notification thresholds and settings.
/// </summary>
public record NotificationSettings
{
    public int? VolumeThreshold { get; private set; }
    public int? CriticalityThreshold { get; private set; }

    public NotificationSettings(int? volumeThreshold = null, int? criticalityThreshold = null)
    {
        VolumeThreshold = volumeThreshold;
        CriticalityThreshold = criticalityThreshold;
    }

    /// <summary>
    /// Creates a default set of notification settings (useful for Organization defaults).
    /// </summary>
    public static NotificationSettings CreateDefault() =>
        new(10, 8); // Default to 10 reports and 8 criticality

    /// <summary>
    /// Creates an empty set of notification settings (useful for Project overrides).
    /// </summary>
    public static NotificationSettings CreateEmpty() =>
        new(null, null);

    /// <summary>
    /// Determines if any settings have been configured.
    /// </summary>
    public bool HasAnySetting =>
        VolumeThreshold.HasValue ||
        CriticalityThreshold.HasValue;
}
