using DayKeeper.Application.Interfaces;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Wraps Ical.Net to expand RFC 5545 RRULE strings into concrete UTC
/// occurrence timestamps, handling DST transitions via NodaTime (bundled
/// with Ical.Net).
/// </summary>
public sealed class IcalNetRecurrenceExpander : IRecurrenceExpander
{
    /// <inheritdoc />
    public IReadOnlyList<DateTime> GetOccurrences(
        string rrule,
        DateTime seriesStart,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        // Convert the UTC series start to local time in the event's timezone
        // so Ical.Net can perform DST-aware expansion.
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(seriesStart, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById(timezone));

        var dtStart = new CalDateTime(
            localStart.Year, localStart.Month, localStart.Day,
            localStart.Hour, localStart.Minute, localStart.Second,
            timezone);

        var pattern = new RecurrencePattern(rrule);

        var calendarEvent = new CalendarEvent
        {
            Start = dtStart,
            RecurrenceRules = [pattern],
        };

        var windowStart = new CalDateTime(
            DateTime.SpecifyKind(rangeStart, DateTimeKind.Utc), "UTC");
        var windowEnd = new CalDateTime(
            DateTime.SpecifyKind(rangeEnd, DateTimeKind.Utc), "UTC");

        return calendarEvent
            .GetOccurrences(windowStart)
            .TakeWhileBefore(windowEnd)
            .Select(o => o.Period.StartTime.AsUtc)
            .Where(utc => utc >= rangeStart && utc < rangeEnd)
            .OrderBy(utc => utc)
            .ToList();
    }
}
