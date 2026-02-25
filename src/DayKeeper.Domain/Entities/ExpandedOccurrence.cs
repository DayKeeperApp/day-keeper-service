namespace DayKeeper.Domain.Entities;

/// <summary>
/// Read-only projection of a single calendar occurrence (either a standalone event
/// or one expanded instance of a recurring series). Never persisted to the database.
/// </summary>
public record ExpandedOccurrence
{
    /// <summary>Identifier of the master <see cref="CalendarEvent"/>.</summary>
    public required Guid CalendarEventId { get; init; }

    /// <summary>
    /// Identifier of the <see cref="RecurrenceException"/> that modified this occurrence.
    /// <c>null</c> if the occurrence uses the master event's values unmodified.
    /// </summary>
    public Guid? RecurrenceExceptionId { get; init; }

    /// <summary>
    /// Computed start time from the RRULE expansion, before any exception overrides.
    /// For single (non-recurring) events, this equals <see cref="StartAt"/>.
    /// </summary>
    public required DateTime OriginalStartAt { get; init; }

    /// <summary>Effective title for this occurrence.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Effective description for this occurrence.
    /// <c>null</c> if neither the master nor the exception provides a description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>Effective start time (UTC) for this occurrence.</summary>
    public required DateTime StartAt { get; init; }

    /// <summary>Effective end time (UTC) for this occurrence.</summary>
    public required DateTime EndAt { get; init; }

    /// <summary>Whether this is an all-day event (from the master).</summary>
    public bool IsAllDay { get; init; }

    /// <summary>
    /// Start date for all-day events (from the master).
    /// <c>null</c> for timed events.
    /// </summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>
    /// End date for all-day events (from the master).
    /// <c>null</c> for timed events.
    /// </summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>IANA timezone identifier from the master event.</summary>
    public required string Timezone { get; init; }

    /// <summary>
    /// Effective location for this occurrence.
    /// <c>null</c> if neither the master nor the exception specifies a location.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>Calendar that owns the master event.</summary>
    public required Guid CalendarId { get; init; }

    /// <summary>
    /// Event type of the master event.
    /// <c>null</c> if no event type is assigned.
    /// </summary>
    public Guid? EventTypeId { get; init; }

    /// <summary>
    /// <c>true</c> if this occurrence belongs to a recurring series;
    /// <c>false</c> for standalone single events.
    /// </summary>
    public bool IsRecurring { get; init; }

    /// <summary>
    /// <c>true</c> if this occurrence has been modified by a <see cref="RecurrenceException"/>;
    /// <c>false</c> if it uses the master event's values or is a single event.
    /// </summary>
    public bool IsException { get; init; }
}
