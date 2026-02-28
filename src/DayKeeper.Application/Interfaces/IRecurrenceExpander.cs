namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Abstracts RFC 5545 RRULE expansion so the Application layer is
/// decoupled from any specific iCalendar library.
/// </summary>
public interface IRecurrenceExpander
{
    /// <summary>
    /// Expands an RRULE string into concrete UTC occurrence timestamps
    /// within the specified query window.
    /// </summary>
    /// <param name="rrule">RFC 5545 RRULE string (e.g. "FREQ=WEEKLY;BYDAY=MO").</param>
    /// <param name="seriesStart">DTSTART of the series in UTC.</param>
    /// <param name="timezone">IANA timezone identifier for DST-aware expansion.</param>
    /// <param name="rangeStart">Inclusive start of the query window (UTC).</param>
    /// <param name="rangeEnd">Exclusive end of the query window (UTC).</param>
    /// <returns>Sorted list of UTC timestamps for each occurrence in the range.</returns>
    IReadOnlyList<DateTime> GetOccurrences(
        string rrule,
        DateTime seriesStart,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd);
}
