using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing per-device notification preferences.
/// </summary>
public interface IDeviceNotificationPreferenceService
{
    /// <summary>
    /// Retrieves the notification preferences for the specified device.
    /// </summary>
    Task<DeviceNotificationPreference> GetByDeviceIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates the notification preferences for the specified device.
    /// Pass <c>null</c> to leave a field unchanged.
    /// </summary>
    Task<DeviceNotificationPreference> UpdateAsync(
        Guid deviceId,
        bool? dndEnabled,
        TimeOnly? dndStartTime,
        TimeOnly? dndEndTime,
        ReminderLeadTime? defaultReminderLeadTime,
        NotificationSound? notificationSound,
        bool? notifyEvents,
        bool? notifyTasks,
        bool? notifyLists,
        bool? notifyPeople,
        CancellationToken cancellationToken = default);
}
