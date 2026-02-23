namespace DayKeeper.Domain.Enums;

/// <summary>
/// Preferred first day of the week for calendar display.
/// </summary>
public enum WeekStart
{
    /// <summary>Week begins on Sunday.</summary>
    Sunday = 0,

    /// <summary>Week begins on Monday.</summary>
    Monday = 1,

    /// <summary>Week begins on Saturday.</summary>
    Saturday = 6,
}
