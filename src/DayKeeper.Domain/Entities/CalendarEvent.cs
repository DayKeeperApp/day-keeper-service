namespace DayKeeper.Domain.Entities;

/// <summary>
/// A calendar event within a <see cref="Calendar"/>, optionally categorized by an <see cref="EventType"/>.
/// Supports timed events, all-day events, and recurrence via iCalendar RRULE.
/// </summary>
/// <remarks>
/// Named <c>CalendarEvent</c> rather than <c>Event</c> to avoid conflict with the
/// reserved keyword <c>event</c> in C# and other .NET languages (CA1716).
/// </remarks>
public class CalendarEvent : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Calendar"/>.</summary>
    public Guid CalendarId { get; set; }

    /// <summary>
    /// Optional foreign key to an <see cref="EventType"/> for categorization.
    /// <c>null</c> if no event type is assigned.
    /// </summary>
    public Guid? EventTypeId { get; set; }

    /// <summary>Short title describing the event.</summary>
    public required string Title { get; set; }

    /// <summary>
    /// Optional longer description providing additional context for the event.
    /// <c>null</c> if no description has been provided.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this is an all-day event.
    /// When <c>true</c>, <see cref="StartDate"/> and <see cref="EndDate"/> carry the
    /// meaningful date range; when <c>false</c>, <see cref="StartAt"/> and <see cref="EndAt"/>
    /// carry the precise UTC timestamps.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>UTC timestamp indicating when the event starts.</summary>
    public DateTime StartAt { get; set; }

    /// <summary>UTC timestamp indicating when the event ends.</summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Start date for all-day events.
    /// <c>null</c> if this is not an all-day event.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End date for all-day events.
    /// <c>null</c> if this is not an all-day event.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>IANA timezone identifier (e.g. "America/Chicago") for the event.</summary>
    public required string Timezone { get; set; }

    /// <summary>
    /// Optional iCalendar RRULE string defining the recurrence pattern (e.g. "FREQ=WEEKLY;BYDAY=MO").
    /// <c>null</c> if the event does not recur.
    /// </summary>
    public string? RecurrenceRule { get; set; }

    /// <summary>
    /// Optional location or venue for the event.
    /// <c>null</c> if no location is specified.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>Navigation to the owning calendar.</summary>
    public Calendar Calendar { get; set; } = null!;

    /// <summary>
    /// Navigation to the associated event type.
    /// <c>null</c> if no event type is assigned.
    /// </summary>
    public EventType? EventType { get; set; }

    /// <summary>Reminders configured for this event.</summary>
    public ICollection<EventReminder> Reminders { get; set; } = [];

    /// <summary>File attachments associated with this event.</summary>
    public ICollection<Attachment> Attachments { get; set; } = [];
}
