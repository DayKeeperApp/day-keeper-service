using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing calendar events.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="CalendarEvent"/> entities, including recurrence expansion.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Creates a new event within the specified calendar.
    /// </summary>
    /// <param name="calendarId">The calendar under which to create the event.</param>
    /// <param name="title">Short title describing the event.</param>
    /// <param name="description">Optional longer description.</param>
    /// <param name="isAllDay">Whether this is an all-day event.</param>
    /// <param name="startAt">UTC start timestamp.</param>
    /// <param name="endAt">UTC end timestamp.</param>
    /// <param name="startDate">Start date for all-day events, or <c>null</c>.</param>
    /// <param name="endDate">End date for all-day events, or <c>null</c>.</param>
    /// <param name="timezone">IANA timezone identifier (e.g. "America/Chicago").</param>
    /// <param name="recurrenceRule">Optional iCalendar RRULE string.</param>
    /// <param name="recurrenceEndAt">Denormalized UTC end boundary of the recurrence series, or <c>null</c>.</param>
    /// <param name="location">Optional location/venue.</param>
    /// <param name="eventTypeId">Optional event type for categorization.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created calendar event.</returns>
    /// <exception cref="EntityNotFoundException">The calendar or event type does not exist.</exception>
    Task<CalendarEvent> CreateEventAsync(
        Guid calendarId,
        string title,
        string? description,
        bool isAllDay,
        DateTime startAt,
        DateTime endAt,
        DateOnly? startDate,
        DateOnly? endDate,
        string timezone,
        string? recurrenceRule,
        DateTime? recurrenceEndAt,
        string? location,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates properties of an existing calendar event. All nullable parameters
    /// represent optional partial updates; <c>null</c> means "leave unchanged".
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to update.</param>
    /// <param name="title">The new title, or <c>null</c> to leave unchanged.</param>
    /// <param name="description">The new description, or <c>null</c> to leave unchanged.</param>
    /// <param name="isAllDay">The new all-day flag, or <c>null</c> to leave unchanged.</param>
    /// <param name="startAt">The new UTC start time, or <c>null</c> to leave unchanged.</param>
    /// <param name="endAt">The new UTC end time, or <c>null</c> to leave unchanged.</param>
    /// <param name="startDate">The new start date, or <c>null</c> to leave unchanged.</param>
    /// <param name="endDate">The new end date, or <c>null</c> to leave unchanged.</param>
    /// <param name="timezone">The new timezone, or <c>null</c> to leave unchanged.</param>
    /// <param name="recurrenceRule">The new RRULE, or <c>null</c> to leave unchanged.</param>
    /// <param name="recurrenceEndAt">The new recurrence end boundary, or <c>null</c> to leave unchanged.</param>
    /// <param name="location">The new location, or <c>null</c> to leave unchanged.</param>
    /// <param name="eventTypeId">The new event type ID, or <c>null</c> to leave unchanged. Pass <see cref="Guid.Empty"/> to unassign.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated calendar event.</returns>
    /// <exception cref="EntityNotFoundException">The event or event type does not exist.</exception>
    Task<CalendarEvent> UpdateEventAsync(
        Guid eventId,
        string? title,
        string? description,
        bool? isAllDay,
        DateTime? startAt,
        DateTime? endAt,
        DateOnly? startDate,
        DateOnly? endDate,
        string? timezone,
        string? recurrenceRule,
        DateTime? recurrenceEndAt,
        string? location,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a calendar event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the event was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all event occurrences within a time range for one or more calendars,
    /// expanding recurring events via RRULE and applying recurrence exceptions.
    /// Cancelled exceptions are excluded; modified exceptions override the base occurrence.
    /// </summary>
    /// <param name="calendarIds">The calendars whose events to include.</param>
    /// <param name="rangeStart">Inclusive start of the query window (UTC).</param>
    /// <param name="rangeEnd">Exclusive end of the query window (UTC).</param>
    /// <param name="timezone">IANA timezone identifier for DST-aware recurrence expansion.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Expanded occurrences sorted by <see cref="ExpandedOccurrence.StartAt"/>.</returns>
    Task<IReadOnlyList<ExpandedOccurrence>> GetEventsForRangeAsync(
        IEnumerable<Guid> calendarIds,
        DateTime rangeStart,
        DateTime rangeEnd,
        string timezone,
        CancellationToken cancellationToken = default);
}
