using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with system reference data and optional development fixtures.
/// All operations are idempotent — safe to call on every application startup.
/// </summary>
public static partial class DbInitializer
{
    // ── System EventType IDs (deterministic) ────────────────
    private static readonly Guid _eventTypeBirthdayId = new("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid _eventTypeHolidayId = new("a0000000-0000-0000-0000-000000000002");
    private static readonly Guid _eventTypeAppointmentId = new("a0000000-0000-0000-0000-000000000003");
    private static readonly Guid _eventTypeMeetingId = new("a0000000-0000-0000-0000-000000000004");
    private static readonly Guid _eventTypeReminderId = new("a0000000-0000-0000-0000-000000000005");
    private static readonly Guid _eventTypeDeadlineId = new("a0000000-0000-0000-0000-000000000006");
    private static readonly Guid _eventTypeSocialId = new("a0000000-0000-0000-0000-000000000007");
    private static readonly Guid _eventTypeTravelId = new("a0000000-0000-0000-0000-000000000008");
    private static readonly Guid _eventTypeWorkId = new("a0000000-0000-0000-0000-000000000009");
    private static readonly Guid _eventTypePersonalId = new("a0000000-0000-0000-0000-00000000000a");
    private static readonly Guid _eventTypeAnniversaryId = new("a0000000-0000-0000-0000-00000000000b");

    // ── System Category IDs (deterministic) ─────────────────
    private static readonly Guid _categoryErrandsId = new("b0000000-0000-0000-0000-000000000001");
    private static readonly Guid _categoryHealthId = new("b0000000-0000-0000-0000-000000000002");
    private static readonly Guid _categoryFinanceId = new("b0000000-0000-0000-0000-000000000003");
    private static readonly Guid _categoryHomeId = new("b0000000-0000-0000-0000-000000000004");
    private static readonly Guid _categoryWorkId = new("b0000000-0000-0000-0000-000000000005");
    private static readonly Guid _categoryPersonalId = new("b0000000-0000-0000-0000-000000000006");
    private static readonly Guid _categoryShoppingId = new("b0000000-0000-0000-0000-000000000007");
    private static readonly Guid _categoryFitnessId = new("b0000000-0000-0000-0000-000000000008");
    private static readonly Guid _categoryLearningId = new("b0000000-0000-0000-0000-000000000009");
    private static readonly Guid _categorySocialId = new("b0000000-0000-0000-0000-00000000000a");

    // ── System Holidays Space/Calendar/Event IDs (deterministic) ──
    private static readonly Guid _systemHolidaysSpaceId = new("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid _systemHolidaysCalendarId = new("c0000000-0000-0000-0000-000000000002");
    private static readonly Guid _eventNewYearsDayId = new("c0000000-0000-0000-0000-000000000010");
    private static readonly Guid _eventMlkDayId = new("c0000000-0000-0000-0000-000000000011");
    private static readonly Guid _eventPresidentsDayId = new("c0000000-0000-0000-0000-000000000012");
    private static readonly Guid _eventMemorialDayId = new("c0000000-0000-0000-0000-000000000013");
    private static readonly Guid _eventJuneteenthId = new("c0000000-0000-0000-0000-000000000014");
    private static readonly Guid _eventIndependenceDayId = new("c0000000-0000-0000-0000-000000000015");
    private static readonly Guid _eventLaborDayId = new("c0000000-0000-0000-0000-000000000016");
    private static readonly Guid _eventColumbusDayId = new("c0000000-0000-0000-0000-000000000017");
    private static readonly Guid _eventVeteransDayId = new("c0000000-0000-0000-0000-000000000018");
    private static readonly Guid _eventThanksgivingId = new("c0000000-0000-0000-0000-000000000019");
    private static readonly Guid _eventChristmasDayId = new("c0000000-0000-0000-0000-00000000001a");

    // ── Development fixture IDs (deterministic) ─────────────
    private static readonly Guid _devTenantId = new("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid _devUserId = new("d0000000-0000-0000-0000-000000000002");
    private static readonly Guid _devSpaceId = new("d0000000-0000-0000-0000-000000000003");
    private static readonly Guid _devMembershipId = new("d0000000-0000-0000-0000-000000000004");
    private static readonly Guid _devCalendarId = new("d0000000-0000-0000-0000-000000000005");

    public static async Task SeedAsync(
        DayKeeperDbContext context,
        bool isDevelopment,
        ILogger logger,
        CancellationToken ct = default)
    {
        await SeedSystemEventTypesAsync(context, logger, ct).ConfigureAwait(false);
        await SeedSystemCategoriesAsync(context, logger, ct).ConfigureAwait(false);
        await SeedSystemHolidaysAsync(context, logger, ct).ConfigureAwait(false);

        if (isDevelopment)
        {
            await SeedDevelopmentDataAsync(context, logger, ct).ConfigureAwait(false);
        }
    }

    private static async Task SeedSystemEventTypesAsync(
        DayKeeperDbContext context,
        ILogger logger,
        CancellationToken ct)
    {
        var existingIds = await context.Set<EventType>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == null)
            .Select(e => e.Id)
            .ToHashSetAsync(ct)
            .ConfigureAwait(false);

        var toAdd = GetSystemEventTypes()
            .Where(e => !existingIds.Contains(e.Id))
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        context.Set<EventType>().AddRange(toAdd);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        LogSeededEventTypes(logger, toAdd.Count);
    }

    private static async Task SeedSystemCategoriesAsync(
        DayKeeperDbContext context,
        ILogger logger,
        CancellationToken ct)
    {
        var existingIds = await context.Set<Category>()
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == null)
            .Select(c => c.Id)
            .ToHashSetAsync(ct)
            .ConfigureAwait(false);

        var toAdd = GetSystemCategories()
            .Where(c => !existingIds.Contains(c.Id))
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        context.Set<Category>().AddRange(toAdd);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        LogSeededCategories(logger, toAdd.Count);
    }

    private static async Task SeedSystemHolidaysAsync(
        DayKeeperDbContext context,
        ILogger logger,
        CancellationToken ct)
    {
        var spaceExists = await context.Set<Space>()
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Id == _systemHolidaysSpaceId, ct)
            .ConfigureAwait(false);

        if (spaceExists)
        {
            return;
        }

        context.Set<Space>().Add(new Space
        {
            Id = _systemHolidaysSpaceId,
            TenantId = null,
            Name = "Holidays",
            NormalizedName = "holidays",
            SpaceType = SpaceType.System,
        });

        context.Set<Calendar>().Add(new Calendar
        {
            Id = _systemHolidaysCalendarId,
            SpaceId = _systemHolidaysSpaceId,
            Name = "US Holidays",
            NormalizedName = "us holidays",
            Color = "#FFD93D",
            IsDefault = true,
        });

        context.Set<CalendarEvent>().AddRange(GetSystemHolidayEvents());

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        LogSeededHolidays(logger, 11);
    }

    private static async Task SeedDevelopmentDataAsync(
        DayKeeperDbContext context,
        ILogger logger,
        CancellationToken ct)
    {
        var tenantExists = await context.Set<Tenant>()
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == _devTenantId, ct)
            .ConfigureAwait(false);

        if (tenantExists)
        {
            return;
        }

        context.Set<Tenant>().Add(new Tenant
        {
            Id = _devTenantId,
            Name = "Development",
            Slug = "dev",
        });

        context.Set<User>().Add(new User
        {
            Id = _devUserId,
            TenantId = _devTenantId,
            DisplayName = "Dev User",
            Email = "dev@daykeeper.local",
            Timezone = "America/Chicago",
            WeekStart = WeekStart.Sunday,
            Locale = "en-US",
        });

        context.Set<Space>().Add(new Space
        {
            Id = _devSpaceId,
            TenantId = _devTenantId,
            Name = "Personal",
            NormalizedName = "personal",
            SpaceType = SpaceType.Personal,
        });

        context.Set<SpaceMembership>().Add(new SpaceMembership
        {
            Id = _devMembershipId,
            SpaceId = _devSpaceId,
            UserId = _devUserId,
            Role = SpaceRole.Owner,
        });

        context.Set<Calendar>().Add(new Calendar
        {
            Id = _devCalendarId,
            SpaceId = _devSpaceId,
            Name = "My Calendar",
            NormalizedName = "my calendar",
            Color = "#4A90D9",
            IsDefault = true,
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        LogSeededDevTenant(logger, _devTenantId);
    }

    private static List<EventType> GetSystemEventTypes() =>
    [
        new() { Id = _eventTypeBirthdayId, TenantId = null, Name = "Birthday", NormalizedName = "birthday", Color = "#FF6B8A", Icon = "cake" },
        new() { Id = _eventTypeHolidayId, TenantId = null, Name = "Holiday", NormalizedName = "holiday", Color = "#FFD93D", Icon = "celebration" },
        new() { Id = _eventTypeAppointmentId, TenantId = null, Name = "Appointment", NormalizedName = "appointment", Color = "#4A90D9", Icon = "event" },
        new() { Id = _eventTypeMeetingId, TenantId = null, Name = "Meeting", NormalizedName = "meeting", Color = "#6C5CE7", Icon = "groups" },
        new() { Id = _eventTypeReminderId, TenantId = null, Name = "Reminder", NormalizedName = "reminder", Color = "#00B894", Icon = "notifications" },
        new() { Id = _eventTypeDeadlineId, TenantId = null, Name = "Deadline", NormalizedName = "deadline", Color = "#E17055", Icon = "flag" },
        new() { Id = _eventTypeSocialId, TenantId = null, Name = "Social", NormalizedName = "social", Color = "#FDCB6E", Icon = "people" },
        new() { Id = _eventTypeTravelId, TenantId = null, Name = "Travel", NormalizedName = "travel", Color = "#0984E3", Icon = "flight" },
        new() { Id = _eventTypeWorkId, TenantId = null, Name = "Work", NormalizedName = "work", Color = "#636E72", Icon = "work" },
        new() { Id = _eventTypePersonalId, TenantId = null, Name = "Personal", NormalizedName = "personal", Color = "#A29BFE", Icon = "person" },
        new() { Id = _eventTypeAnniversaryId, TenantId = null, Name = "Anniversary", NormalizedName = "anniversary", Color = "#E84393", Icon = "favorite" },
    ];

    private static List<Category> GetSystemCategories() =>
    [
        new() { Id = _categoryErrandsId, TenantId = null, Name = "Errands", NormalizedName = "errands", Color = "#FF6B6B", Icon = "directions_run" },
        new() { Id = _categoryHealthId, TenantId = null, Name = "Health", NormalizedName = "health", Color = "#51CF66", Icon = "favorite" },
        new() { Id = _categoryFinanceId, TenantId = null, Name = "Finance", NormalizedName = "finance", Color = "#339AF0", Icon = "account_balance" },
        new() { Id = _categoryHomeId, TenantId = null, Name = "Home", NormalizedName = "home", Color = "#FF922B", Icon = "home" },
        new() { Id = _categoryWorkId, TenantId = null, Name = "Work", NormalizedName = "work", Color = "#636E72", Icon = "work" },
        new() { Id = _categoryPersonalId, TenantId = null, Name = "Personal", NormalizedName = "personal", Color = "#A29BFE", Icon = "person" },
        new() { Id = _categoryShoppingId, TenantId = null, Name = "Shopping", NormalizedName = "shopping", Color = "#F06595", Icon = "shopping_cart" },
        new() { Id = _categoryFitnessId, TenantId = null, Name = "Fitness", NormalizedName = "fitness", Color = "#20C997", Icon = "fitness_center" },
        new() { Id = _categoryLearningId, TenantId = null, Name = "Learning", NormalizedName = "learning", Color = "#845EF7", Icon = "school" },
        new() { Id = _categorySocialId, TenantId = null, Name = "Social", NormalizedName = "social", Color = "#FDCB6E", Icon = "people" },
    ];

    private static List<CalendarEvent> GetSystemHolidayEvents() =>
    [
        Holiday(_eventNewYearsDayId, "New Year's Day", 2024, 1, 1, "FREQ=YEARLY;BYMONTH=1;BYMONTHDAY=1"),
        Holiday(_eventMlkDayId, "Martin Luther King Jr. Day", 2024, 1, 15, "FREQ=YEARLY;BYMONTH=1;BYDAY=3MO"),
        Holiday(_eventPresidentsDayId, "Presidents' Day", 2024, 2, 19, "FREQ=YEARLY;BYMONTH=2;BYDAY=3MO"),
        Holiday(_eventMemorialDayId, "Memorial Day", 2024, 5, 27, "FREQ=YEARLY;BYMONTH=5;BYDAY=-1MO"),
        Holiday(_eventJuneteenthId, "Juneteenth", 2024, 6, 19, "FREQ=YEARLY;BYMONTH=6;BYMONTHDAY=19"),
        Holiday(_eventIndependenceDayId, "Independence Day", 2024, 7, 4, "FREQ=YEARLY;BYMONTH=7;BYMONTHDAY=4"),
        Holiday(_eventLaborDayId, "Labor Day", 2024, 9, 2, "FREQ=YEARLY;BYMONTH=9;BYDAY=1MO"),
        Holiday(_eventColumbusDayId, "Columbus Day / Indigenous Peoples' Day", 2024, 10, 14, "FREQ=YEARLY;BYMONTH=10;BYDAY=2MO"),
        Holiday(_eventVeteransDayId, "Veterans Day", 2024, 11, 11, "FREQ=YEARLY;BYMONTH=11;BYMONTHDAY=11"),
        Holiday(_eventThanksgivingId, "Thanksgiving Day", 2024, 11, 28, "FREQ=YEARLY;BYMONTH=11;BYDAY=4TH"),
        Holiday(_eventChristmasDayId, "Christmas Day", 2024, 12, 25, "FREQ=YEARLY;BYMONTH=12;BYMONTHDAY=25"),
    ];

    private static CalendarEvent Holiday(Guid id, string title, int year, int month, int day, string rrule) => new()
    {
        Id = id,
        CalendarId = _systemHolidaysCalendarId,
        EventTypeId = _eventTypeHolidayId,
        Title = title,
        IsAllDay = true,
        Timezone = "UTC",
        StartAt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc),
        EndAt = new DateTime(year, month, day, 23, 59, 59, DateTimeKind.Utc),
        StartDate = new DateOnly(year, month, day),
        EndDate = new DateOnly(year, month, day),
        RecurrenceRule = rrule,
    };

    // ── LoggerMessage delegates ─────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Seeded {Count} system event types.")]
    private static partial void LogSeededEventTypes(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Seeded {Count} system categories.")]
    private static partial void LogSeededCategories(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Seeded system holidays space with calendar and {Count} holiday events.")]
    private static partial void LogSeededHolidays(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Seeded development tenant {TenantId} with user, space, and calendar.")]
    private static partial void LogSeededDevTenant(ILogger logger, Guid tenantId);
}
