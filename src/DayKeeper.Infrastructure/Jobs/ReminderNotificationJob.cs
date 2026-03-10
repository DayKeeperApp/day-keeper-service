using DayKeeper.Application.DTOs.Notifications;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DayKeeper.Infrastructure.Jobs;

/// <summary>
/// Quartz job that fires when a scheduled reminder is due.
/// Loads the reminder context from the database and dispatches
/// push notifications to all members of the owning space.
/// </summary>
public sealed partial class ReminderNotificationJob(
    ILogger<ReminderNotificationJob> logger,
    DbContext dbContext,
    INotificationSender notificationSender) : IJob
{
    /// <summary>
    /// Key used in the Quartz <see cref="JobDataMap"/> to store the reminder identifier.
    /// </summary>
    public const string ReminderIdKey = "ReminderId";

    private readonly ILogger<ReminderNotificationJob> _logger = logger;
    private readonly DbContext _dbContext = dbContext;
    private readonly INotificationSender _notificationSender = notificationSender;

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var reminderIdString = dataMap.ContainsKey(ReminderIdKey) ? dataMap.GetString(ReminderIdKey) : null;

        if (!Guid.TryParse(reminderIdString, out var reminderId))
        {
            LogInvalidReminderId(_logger, reminderIdString);
            return;
        }

        LogReminderFired(_logger, reminderId, context.FireTimeUtc);

        var reminder = await LoadReminderWithDevicesAsync(reminderId).ConfigureAwait(false);

        if (reminder is null)
        {
            LogReminderNotFound(_logger, reminderId);
            return;
        }

        if (reminder.Method != ReminderMethod.Push)
        {
            LogNonPushReminder(_logger, reminderId, reminder.Method);
            return;
        }

        await DispatchPushNotificationAsync(reminder, context.CancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<EventReminder?> LoadReminderWithDevicesAsync(Guid reminderId)
    {
        return await _dbContext.Set<EventReminder>()
            .Include(r => r.CalendarEvent)
                .ThenInclude(e => e.Calendar)
                    .ThenInclude(c => c.Space)
                        .ThenInclude(s => s.Memberships)
                            .ThenInclude(m => m.User)
                                .ThenInclude(u => u.Devices)
                                    .ThenInclude(d => d.NotificationPreference)
            .FirstOrDefaultAsync(r => r.Id == reminderId)
            .ConfigureAwait(false);
    }

    private async Task DispatchPushNotificationAsync(
        EventReminder reminder,
        CancellationToken cancellationToken)
    {
        var calendarEvent = reminder.CalendarEvent;
        var space = calendarEvent.Calendar.Space;

        var utcNow = TimeOnly.FromDateTime(DateTime.UtcNow);

        var fcmTokens = space.Memberships
            .SelectMany(m => m.User.Devices)
            .Where(d => ShouldNotify(d, utcNow))
            .Select(d => d.FcmToken)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (fcmTokens.Count == 0)
        {
            LogNoDevices(_logger, reminder.Id, space.Id);
            return;
        }

        var notification = new PushNotification(
            Title: calendarEvent.Title,
            Body: $"Starting in {reminder.MinutesBefore} minutes",
            Data: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["eventId"] = calendarEvent.Id.ToString(),
                ["calendarId"] = calendarEvent.CalendarId.ToString(),
                ["reminderId"] = reminder.Id.ToString(),
            });

        var result = await _notificationSender
            .SendAsync(fcmTokens, notification, cancellationToken)
            .ConfigureAwait(false);

        if (result.StaleTokens.Count > 0)
        {
            await RemoveStaleDevicesAsync(result.StaleTokens).ConfigureAwait(false);
        }

        LogDispatchComplete(_logger, reminder.Id, result.SuccessCount, result.FailureCount);
    }

    private static bool ShouldNotify(Device device, TimeOnly utcNow)
    {
        var pref = device.NotificationPreference;

        // No preferences means defaults apply (notify_events = true, DND disabled).
        if (pref is null)
        {
            return true;
        }

        if (!pref.NotifyEvents)
        {
            return false;
        }

        if (pref.DndEnabled)
        {
            var start = pref.DndStartTime;
            var end = pref.DndEndTime;

            // Midnight-spanning: e.g. 22:00 → 07:00
            bool inDndWindow = start > end
                ? utcNow >= start || utcNow < end
                : utcNow >= start && utcNow < end;

            if (inDndWindow)
            {
                return false;
            }
        }

        return true;
    }

    private async Task RemoveStaleDevicesAsync(IReadOnlyList<string> staleTokens)
    {
        var staleTokenSet = staleTokens.ToHashSet(StringComparer.Ordinal);
        var devices = await _dbContext.Set<Device>()
            .Where(d => staleTokenSet.Contains(d.FcmToken))
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var device in devices)
        {
            device.DeletedAt = DateTime.UtcNow;
        }

        if (devices.Count > 0)
        {
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            LogStaleDevicesRemoved(_logger, devices.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "ReminderNotificationJob fired with invalid or missing ReminderId in JobDataMap. Raw value: {RawValue}")]
    private static partial void LogInvalidReminderId(ILogger logger, string? rawValue);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Reminder {ReminderId} fired at {FireTimeUtc}.")]
    private static partial void LogReminderFired(ILogger logger, Guid reminderId, DateTimeOffset fireTimeUtc);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Reminder {ReminderId} not found in database; it may have been deleted.")]
    private static partial void LogReminderNotFound(ILogger logger, Guid reminderId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Reminder {ReminderId} has method {Method}; skipping push dispatch.")]
    private static partial void LogNonPushReminder(ILogger logger, Guid reminderId, ReminderMethod method);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No devices found for reminder {ReminderId} in space {SpaceId}.")]
    private static partial void LogNoDevices(ILogger logger, Guid reminderId, Guid spaceId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Reminder {ReminderId} dispatch complete. Success: {SuccessCount}, Failure: {FailureCount}.")]
    private static partial void LogDispatchComplete(ILogger logger, Guid reminderId,
        int successCount, int failureCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Soft-deleted {Count} devices with stale FCM tokens.")]
    private static partial void LogStaleDevicesRemoved(ILogger logger, int count);
}
