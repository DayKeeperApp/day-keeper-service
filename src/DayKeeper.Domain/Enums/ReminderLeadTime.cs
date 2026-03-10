namespace DayKeeper.Domain.Enums;

/// <summary>
/// Default lead time before an event at which a reminder notification is sent.
/// </summary>
public enum ReminderLeadTime
{
    /// <summary>No automatic reminder.</summary>
    None = 0,

    /// <summary>Five minutes before the event.</summary>
    FiveMin = 1,

    /// <summary>Fifteen minutes before the event.</summary>
    FifteenMin = 2,

    /// <summary>Thirty minutes before the event.</summary>
    ThirtyMin = 3,

    /// <summary>One hour before the event.</summary>
    OneHour = 4,

    /// <summary>One day before the event.</summary>
    OneDay = 5,
}
