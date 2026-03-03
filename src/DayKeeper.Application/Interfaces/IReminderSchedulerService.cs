namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Schedules and cancels timed reminder notifications via the background scheduler.
/// The caller is responsible for computing the UTC fire time from the event start
/// time and the reminder's <c>MinutesBefore</c> offset.
/// </summary>
public interface IReminderSchedulerService
{
    /// <summary>
    /// Schedules a reminder to fire at the specified UTC time.
    /// If a job for this <paramref name="reminderId"/> already exists, it is replaced.
    /// </summary>
    /// <param name="reminderId">The unique identifier of the <see cref="Domain.Entities.EventReminder"/>.</param>
    /// <param name="fireAtUtc">The UTC time at which the reminder should fire.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task ScheduleReminderAsync(Guid reminderId, DateTime fireAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a previously scheduled reminder. No-op if the reminder is not currently scheduled.
    /// </summary>
    /// <param name="reminderId">The unique identifier of the <see cref="Domain.Entities.EventReminder"/> to cancel.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task CancelReminderAsync(Guid reminderId, CancellationToken cancellationToken = default);
}
