using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class CalendarPowerUserPersona : IPersona
{
    public string Name => "CalendarPowerUser";

    public async Task SeedAsync(PersonaContext ctx, CancellationToken ct)
    {
        var calendarCount = ctx.DataFactory.RandomInt(2, 3);
        for (var i = 0; i < calendarCount; i++)
        {
            await SeedCalendarAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        var roll = ctx.DataFactory.RandomInt(0, 99);
        try
        {
            if (roll < 25)
            {
                await CreateCalendarEventAsync(ctx, allowRecurrence: false, ct).ConfigureAwait(false);
            }
            else if (roll < 40)
            {
                await CreateCalendarEventAsync(ctx, allowRecurrence: true, ct).ConfigureAwait(false);
            }
            else if (roll < 60)
            {
                await GetEventsForRangeAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 75)
            {
                await UpdateCalendarEventAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 85)
            {
                await GetRecurringOccurrencesAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 90)
            {
                await CreateCalendarAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 95)
            {
                await DeleteCalendarEventAsync(ctx, ct).ConfigureAwait(false);
            }
            else
            {
                await GetCalendarsAsync(ctx, ct).ConfigureAwait(false);
            }
        }
        catch (GraphQLException)
        {
            // error already recorded in metrics
        }
        catch (HttpRequestException)
        {
            // error already recorded in metrics
        }
    }

    private static async Task SeedCalendarAsync(PersonaContext ctx, CancellationToken ct)
    {
        var calendarId = await CreateCalendarAsync(ctx, ct).ConfigureAwait(false);
        if (calendarId == Guid.Empty)
        {
            return;
        }

        var eventCount = ctx.DataFactory.RandomInt(30, 50);
        for (var j = 0; j < eventCount; j++)
        {
            await CreateCalendarEventWithIdAsync(ctx, calendarId, allowRecurrence: ctx.DataFactory.RandomBool(0.3f), ct).ConfigureAwait(false);
        }
    }

    private static async Task<Guid> CreateCalendarAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            var spaceId = ctx.GetWorkingSpaceId();
            var (name, color) = ctx.DataFactory.GenerateCalendar();
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateCalendar",
                GraphQLOperations.CreateCalendar,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, name, color, isDefault = false } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createCalendar").GetProperty("calendar").GetProperty("id").GetGuid();
            ctx.CalendarIds.Add(id);
            return id;
        }
        catch (Exception ex) when (ex is GraphQLException or HttpRequestException or InvalidOperationException)
        {
            return Guid.Empty;
        }
    }

    private static async Task CreateCalendarEventAsync(PersonaContext ctx, bool allowRecurrence, CancellationToken ct)
    {
        if (ctx.CalendarIds.IsEmpty)
        {
            return;
        }

        var calendarId = ctx.DataFactory.PickRandom([.. ctx.CalendarIds]);
        await CreateCalendarEventWithIdAsync(ctx, calendarId, allowRecurrence, ct).ConfigureAwait(false);
    }

    private static async Task CreateCalendarEventWithIdAsync(PersonaContext ctx, Guid calendarId, bool allowRecurrence, CancellationToken ct)
    {
        try
        {
            var (title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location)
                = ctx.DataFactory.GenerateCalendarEvent(allowRecurrence);
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateCalendarEvent",
                GraphQLOperations.CreateCalendarEvent,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { calendarId, title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createCalendarEvent").GetProperty("calendarEvent").GetProperty("id").GetGuid();
            ctx.CalendarEventIds.Add(id);
        }
        catch (Exception ex) when (ex is GraphQLException or HttpRequestException or InvalidOperationException)
        {
            // error already recorded in metrics
        }
    }

    private static async Task GetEventsForRangeAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.CalendarIds.IsEmpty)
        {
            return;
        }

        var calendarIds = ctx.CalendarIds.ToArray();
        var rangeStart = DateTime.UtcNow;
        var rangeEnd = rangeStart.AddDays(30);
        var timezone = "UTC";
        await ctx.ApiClient.GraphQLAsync(
            "GetEventsForRange",
            GraphQLOperations.GetEventsForRange,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["calendarIds"] = calendarIds, ["rangeStart"] = rangeStart, ["rangeEnd"] = rangeEnd, ["timezone"] = timezone },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdateCalendarEventAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.CalendarEventIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.CalendarEventIds]);
        var (title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location)
            = ctx.DataFactory.GenerateCalendarEvent(allowRecurrence: false);
        await ctx.ApiClient.GraphQLAsync(
            "UpdateCalendarEvent",
            GraphQLOperations.UpdateCalendarEvent,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, location } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetRecurringOccurrencesAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.CalendarEventIds.IsEmpty)
        {
            return;
        }

        var taskItemId = ctx.DataFactory.PickRandom([.. ctx.CalendarEventIds]);
        var rangeStart = DateTime.UtcNow;
        var rangeEnd = rangeStart.AddDays(90);
        var timezone = "UTC";
        await ctx.ApiClient.GraphQLAsync(
            "GetRecurringOccurrences",
            GraphQLOperations.GetRecurringOccurrences,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["taskItemId"] = taskItemId, ["timezone"] = timezone, ["rangeStart"] = rangeStart, ["rangeEnd"] = rangeEnd },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeleteCalendarEventAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.CalendarEventIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.CalendarEventIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeleteCalendarEvent",
            GraphQLOperations.DeleteCalendarEvent,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetCalendarsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        await ctx.ApiClient.GraphQLAsync(
            "GetCalendars",
            GraphQLOperations.GetCalendars,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }
}
