namespace DayKeeper.Domain.Enums;

/// <summary>
/// Sound profile used for push notifications on a device.
/// </summary>
public enum NotificationSound
{
    /// <summary>Use the system default notification sound.</summary>
    Default = 0,

    /// <summary>Deliver notifications silently.</summary>
    Silent = 1,

    /// <summary>Use a custom notification sound.</summary>
    Custom = 2,
}
