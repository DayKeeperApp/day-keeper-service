using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A reminder associated with a <see cref="CalendarEvent"/>.
/// Each event may have multiple reminders with different lead times and delivery methods.
/// </summary>
public class EventReminder : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="CalendarEvent"/>.</summary>
    public Guid CalendarEventId { get; set; }

    /// <summary>
    /// Number of minutes before the event start time that the reminder should fire.
    /// For example, 15 means the reminder fires 15 minutes before <see cref="CalendarEvent.StartAt"/>.
    /// </summary>
    public int MinutesBefore { get; set; }

    /// <summary>Delivery method for this reminder.</summary>
    public ReminderMethod Method { get; set; }

    /// <summary>Navigation to the owning event.</summary>
    public CalendarEvent CalendarEvent { get; set; } = null!;
}
