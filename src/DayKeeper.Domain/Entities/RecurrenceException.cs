using System.Diagnostics.CodeAnalysis;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Records a modification or cancellation of a single occurrence within a recurring
/// <see cref="CalendarEvent"/> series. The <see cref="OriginalStartAt"/> identifies
/// which occurrence is overridden (standard iCalendar RECURRENCE-ID pattern).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "RecurrenceException is the standard iCalendar (RFC 5545) domain term, not a System.Exception subclass."
)]
public class RecurrenceException : BaseEntity
{
    /// <summary>Foreign key to the recurring master <see cref="CalendarEvent"/>.</summary>
    public Guid CalendarEventId { get; set; }

    /// <summary>
    /// UTC start time of the original occurrence being overridden.
    /// Together with <see cref="CalendarEventId"/>, uniquely identifies the target occurrence.
    /// </summary>
    public DateTime OriginalStartAt { get; set; }

    /// <summary>
    /// When <c>true</c>, the occurrence is cancelled (deleted from the series).
    /// Override fields are ignored for cancelled occurrences.
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Overridden title for this occurrence.
    /// <c>null</c> to inherit from the master event.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Overridden description for this occurrence.
    /// <c>null</c> to inherit from the master event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Overridden start time (UTC) for this occurrence, enabling rescheduling.
    /// <c>null</c> to inherit from the computed RRULE start.
    /// </summary>
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Overridden end time (UTC) for this occurrence.
    /// <c>null</c> to inherit from the computed RRULE end.
    /// </summary>
    public DateTime? EndAt { get; set; }

    /// <summary>
    /// Overridden location for this occurrence.
    /// <c>null</c> to inherit from the master event.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>Navigation to the recurring master event.</summary>
    public CalendarEvent CalendarEvent { get; set; } = null!;
}
