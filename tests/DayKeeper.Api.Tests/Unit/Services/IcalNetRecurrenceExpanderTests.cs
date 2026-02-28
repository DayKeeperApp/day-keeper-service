using DayKeeper.Infrastructure.Services;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class IcalNetRecurrenceExpanderTests
{
    private readonly IcalNetRecurrenceExpander _sut = new();

    // ── Helper: all dates use UTC and a fixed timezone for predictability ──
    private const string _tz = "America/Chicago"; // UTC-6 (CST) / UTC-5 (CDT)

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0, int s = 0) =>
        new(y, m, d, h, min, s, DateTimeKind.Utc);

    // ── Daily recurrence ──────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_DailyRule_ReturnsCorrectDates()
    {
        // Daily at 15:00 UTC (09:00 CST), query a 5-day window
        var seriesStart = Utc(2026, 3, 1, 15, 0, 0);
        var rangeStart = Utc(2026, 3, 1);
        var rangeEnd = Utc(2026, 3, 6);

        var result = _sut.GetOccurrences("FREQ=DAILY", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(5);
        result[0].Should().Be(Utc(2026, 3, 1, 15, 0, 0));
        result[4].Should().Be(Utc(2026, 3, 5, 15, 0, 0));
    }

    // ── Weekly recurrence ─────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_WeeklyRule_ReturnsWeeklyDates()
    {
        // Weekly on Mondays at 14:00 UTC
        var seriesStart = Utc(2026, 3, 2, 14, 0, 0); // Monday
        var rangeStart = Utc(2026, 3, 1);
        var rangeEnd = Utc(2026, 4, 1);

        var result = _sut.GetOccurrences("FREQ=WEEKLY;BYDAY=MO", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(5); // Mar 2, 9, 16, 23, 30
        result.Should().AllSatisfy(d => d.DayOfWeek.Should().Be(DayOfWeek.Monday));
    }

    // ── COUNT limiter ─────────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_WithCount_StopsAfterCount()
    {
        var seriesStart = Utc(2026, 3, 1, 12, 0, 0);
        var rangeStart = Utc(2026, 3, 1);
        var rangeEnd = Utc(2026, 12, 31); // large window

        var result = _sut.GetOccurrences("FREQ=DAILY;COUNT=3", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(3);
    }

    // ── UNTIL limiter ─────────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_WithUntil_StopsAtUntilDate()
    {
        // UNTIL is inclusive per RFC 5545
        var seriesStart = Utc(2026, 3, 1, 12, 0, 0);
        var rangeStart = Utc(2026, 3, 1);
        var rangeEnd = Utc(2026, 4, 1);

        var result = _sut.GetOccurrences(
            "FREQ=DAILY;UNTIL=20260305T120000Z",
            seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(5); // Mar 1–5 inclusive
        result[^1].Should().Be(Utc(2026, 3, 5, 12, 0, 0));
    }

    // ── Empty range ───────────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_NoOccurrencesInRange_ReturnsEmpty()
    {
        var seriesStart = Utc(2026, 6, 1, 10, 0, 0);
        // Query window is entirely before the series starts
        var rangeStart = Utc(2026, 1, 1);
        var rangeEnd = Utc(2026, 2, 1);

        var result = _sut.GetOccurrences("FREQ=DAILY", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().BeEmpty();
    }

    // ── DST spring-forward ────────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_DstSpringForward_AdjustsUtcOffset()
    {
        // America/Chicago: DST starts 2026-03-08 02:00 local (CST->CDT, UTC-6 -> UTC-5)
        // Daily event at 09:00 local = 15:00 UTC (CST) shifts to 14:00 UTC (CDT)
        var seriesStart = Utc(2026, 3, 7, 15, 0, 0); // Mar 7: 09:00 CST = 15:00 UTC
        var rangeStart = Utc(2026, 3, 7);
        var rangeEnd = Utc(2026, 3, 10);

        var result = _sut.GetOccurrences("FREQ=DAILY", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(3);
        result[0].Should().Be(Utc(2026, 3, 7, 15, 0, 0)); // CST: 15:00 UTC
        result[1].Should().Be(Utc(2026, 3, 8, 14, 0, 0)); // CDT: 14:00 UTC
        result[2].Should().Be(Utc(2026, 3, 9, 14, 0, 0)); // CDT: 14:00 UTC
    }

    // ── rangeStart filtering ──────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_RangeStartExclusion_FiltersCorrectly()
    {
        // Series starts before the query window
        var seriesStart = Utc(2026, 3, 1, 12, 0, 0);
        var rangeStart = Utc(2026, 3, 4);
        var rangeEnd = Utc(2026, 3, 7);

        var result = _sut.GetOccurrences("FREQ=DAILY", seriesStart, _tz, rangeStart, rangeEnd);

        result.Should().HaveCount(3); // Mar 4, 5, 6
        result[0].Should().Be(Utc(2026, 3, 4, 12, 0, 0));
    }

    // ── rangeEnd exclusivity ──────────────────────────────────────────────

    [Fact]
    public void GetOccurrences_RangeEndExclusion_IsExclusive()
    {
        // Occurrence falls exactly at rangeEnd — should be excluded
        var seriesStart = Utc(2026, 3, 1, 0, 0, 0);
        var rangeStart = Utc(2026, 3, 1);
        var rangeEnd = Utc(2026, 3, 3, 0, 0, 0);

        var result = _sut.GetOccurrences("FREQ=DAILY", seriesStart, _tz, rangeStart, rangeEnd);

        // Mar 1 00:00 and Mar 2 00:00 are in range; Mar 3 00:00 is exactly at rangeEnd (exclusive)
        result.Should().HaveCount(2);
        result.Should().NotContain(Utc(2026, 3, 3, 0, 0, 0));
    }
}
