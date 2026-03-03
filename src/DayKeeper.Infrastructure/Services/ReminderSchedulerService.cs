using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IReminderSchedulerService"/>
/// backed by Quartz.NET. Schedules one-shot triggers for individual reminder fire times.
/// </summary>
public sealed partial class ReminderSchedulerService(
    ISchedulerFactory schedulerFactory,
    ILogger<ReminderSchedulerService> logger) : IReminderSchedulerService
{
    private const string _reminderGroupName = "reminders";

    private readonly ISchedulerFactory _schedulerFactory = schedulerFactory;
    private readonly ILogger<ReminderSchedulerService> _logger = logger;

    /// <inheritdoc />
    public async Task ScheduleReminderAsync(
        Guid reminderId,
        DateTime fireAtUtc,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken)
            .ConfigureAwait(false);

        var jobKey = CreateJobKey(reminderId);

        var job = JobBuilder.Create<ReminderNotificationJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ReminderNotificationJob.ReminderIdKey, reminderId.ToString())
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"reminder-{reminderId}", _reminderGroupName)
            .StartAt(new DateTimeOffset(fireAtUtc, TimeSpan.Zero))
            .Build();

        // Replace any existing job for this reminder (e.g. event time was updated)
        await scheduler.ScheduleJob(job, [trigger], replace: true, cancellationToken)
            .ConfigureAwait(false);

        LogReminderScheduled(_logger, reminderId, fireAtUtc);
    }

    /// <inheritdoc />
    public async Task CancelReminderAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken)
            .ConfigureAwait(false);

        var jobKey = CreateJobKey(reminderId);
        var deleted = await scheduler.DeleteJob(jobKey, cancellationToken)
            .ConfigureAwait(false);

        if (deleted)
        {
            LogReminderCancelled(_logger, reminderId);
        }
        else
        {
            LogReminderNotFound(_logger, reminderId);
        }
    }

    private static JobKey CreateJobKey(Guid reminderId)
        => new($"reminder-{reminderId}", _reminderGroupName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Scheduled reminder {ReminderId} to fire at {FireAtUtc}.")]
    private static partial void LogReminderScheduled(ILogger logger, Guid reminderId, DateTime fireAtUtc);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cancelled reminder {ReminderId}.")]
    private static partial void LogReminderCancelled(ILogger logger, Guid reminderId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No scheduled job found for reminder {ReminderId}; nothing to cancel.")]
    private static partial void LogReminderNotFound(ILogger logger, Guid reminderId);
}
