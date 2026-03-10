using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Per-device notification preferences controlling how and when push
/// notifications are delivered to a <see cref="Device"/>.
/// </summary>
public class DeviceNotificationPreference : BaseEntity, ITenantScoped
{
    /// <summary>Foreign key to the owning <see cref="Tenant"/>.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Foreign key to the associated <see cref="Device"/>.</summary>
    public Guid DeviceId { get; set; }

    /// <summary>Whether Do-Not-Disturb mode is enabled for this device.</summary>
    public bool DndEnabled { get; set; }

    /// <summary>Start of the DND window in UTC (HH:mm). Notifications are suppressed from this time.</summary>
    public TimeOnly DndStartTime { get; set; } = new(22, 0);

    /// <summary>End of the DND window in UTC (HH:mm). Notifications resume at this time.</summary>
    public TimeOnly DndEndTime { get; set; } = new(7, 0);

    /// <summary>Default lead time for event reminders on this device.</summary>
    public ReminderLeadTime DefaultReminderLeadTime { get; set; } = ReminderLeadTime.FifteenMin;

    /// <summary>Notification sound profile for this device.</summary>
    public NotificationSound NotificationSound { get; set; } = NotificationSound.Default;

    /// <summary>Whether to send push notifications for calendar events.</summary>
    public bool NotifyEvents { get; set; } = true;

    /// <summary>Whether to send push notifications for task updates.</summary>
    public bool NotifyTasks { get; set; } = true;

    /// <summary>Whether to send push notifications for list updates.</summary>
    public bool NotifyLists { get; set; } = true;

    /// <summary>Whether to send push notifications for people/contact updates.</summary>
    public bool NotifyPeople { get; set; }

    /// <summary>Navigation to the owning tenant.</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Navigation to the associated device.</summary>
    public Device Device { get; set; } = null!;
}
