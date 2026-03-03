using Microsoft.Extensions.Logging;
using Quartz;

namespace DayKeeper.Infrastructure.Jobs;

/// <summary>
/// Quartz job that fires when a scheduled reminder is due.
/// Currently logs the reminder ID; actual notification dispatch (FCM, email, in-app)
/// will be implemented in DKS-1vl.
/// </summary>
public sealed partial class ReminderNotificationJob(
    ILogger<ReminderNotificationJob> logger) : IJob
{
    /// <summary>
    /// Key used in the Quartz <see cref="JobDataMap"/> to store the reminder identifier.
    /// </summary>
    public const string ReminderIdKey = "ReminderId";

    private readonly ILogger<ReminderNotificationJob> _logger = logger;

    public Task Execute(IJobExecutionContext context)
    {
        var reminderIdString = context.MergedJobDataMap.GetString(ReminderIdKey);

        if (!Guid.TryParse(reminderIdString, out var reminderId))
        {
            LogInvalidReminderId(_logger, reminderIdString);
            return Task.CompletedTask;
        }

        LogReminderFired(_logger, reminderId, context.FireTimeUtc);

        // DKS-1vl will inject and call the notification sender here:
        // - Look up the EventReminder (and its CalendarEvent) from the database
        // - Determine the ReminderMethod (Push, Email, InApp)
        // - Dispatch accordingly

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "ReminderNotificationJob fired with invalid or missing ReminderId in JobDataMap. Raw value: {RawValue}")]
    private static partial void LogInvalidReminderId(ILogger logger, string? rawValue);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Reminder {ReminderId} fired at {FireTimeUtc}. Notification dispatch not yet implemented (DKS-1vl).")]
    private static partial void LogReminderFired(ILogger logger, Guid reminderId, DateTimeOffset fireTimeUtc);
}
