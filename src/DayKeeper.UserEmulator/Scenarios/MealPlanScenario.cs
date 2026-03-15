using DayKeeper.UserEmulator.Client;
using DayKeeper.UserEmulator.Personas;

namespace DayKeeper.UserEmulator.Scenarios;

public sealed class MealPlanScenario : IScenarioPack
{
    public string Name => "MealPlan";

    public async Task<int> ExecuteAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var listId = await CreateShoppingListAsync(ctx, spaceId, ct).ConfigureAwait(false);
        var calendarId = await CreateCalendarAsync(ctx, spaceId, ct).ConfigureAwait(false);

        var ingredientCount = ctx.DataFactory.RandomInt(20, 30);
        await CreateIngredientsAsync(ctx, listId, ingredientCount, ct).ConfigureAwait(false);
        await CreateCookingEventsAsync(ctx, calendarId, ct).ConfigureAwait(false);
        await CreatePrepTasksAsync(ctx, spaceId, ct).ConfigureAwait(false);

        return 1 + 1 + ingredientCount + 7 + 7;
    }

    private static async Task<Guid> CreateShoppingListAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var name = ctx.DataFactory.GenerateShoppingListName();
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { id = Guid.NewGuid(), name, spaceId },
        };

        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateShoppingList", GraphQLOperations.CreateShoppingList, variables,
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

        var listId = result.GetProperty("createShoppingList").GetProperty("shoppingList").GetProperty("id").GetGuid();
        ctx.ShoppingListIds.Add(listId);
        return listId;
    }

    private static async Task<Guid> CreateCalendarAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var (name, color) = ctx.DataFactory.GenerateCalendar();
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { id = Guid.NewGuid(), name = $"Meal Plan: {name}", color, spaceId, isDefault = false },
        };

        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateCalendar", GraphQLOperations.CreateCalendar, variables,
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

        var calendarId = result.GetProperty("createCalendar").GetProperty("calendar").GetProperty("id").GetGuid();
        ctx.CalendarIds.Add(calendarId);
        return calendarId;
    }

    private static async Task CreateIngredientsAsync(PersonaContext ctx, Guid listId, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            var (name, quantity, unit) = ctx.DataFactory.GenerateListItem();
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new { id = Guid.NewGuid(), name, quantity, unit, shoppingListId = listId, sortOrder = i, isChecked = false },
            };

            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateListItem", GraphQLOperations.CreateListItem, variables,
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

            var itemId = result.GetProperty("createListItem").GetProperty("listItem").GetProperty("id").GetGuid();
            ctx.ListItemIds.Add(itemId);
        }
    }

    private static async Task CreateCookingEventsAsync(PersonaContext ctx, Guid calendarId, CancellationToken ct)
    {
        for (var day = 0; day < 7; day++)
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(day + 1);
            var startAt = baseDate.AddHours(18);
            var endAt = startAt.AddHours(1);

            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new
                {
                    id = Guid.NewGuid(),
                    title = $"Cook Day {day + 1}",
                    isAllDay = false,
                    startAt,
                    endAt,
                    timezone = "UTC",
                    calendarId,
                },
            };

            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateCalendarEvent", GraphQLOperations.CreateCalendarEvent, variables,
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

            var eventId = result.GetProperty("createCalendarEvent").GetProperty("calendarEvent").GetProperty("id").GetGuid();
            ctx.CalendarEventIds.Add(eventId);
        }
    }

    private static async Task CreatePrepTasksAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var prepTitles = new[]
        {
            "Buy groceries", "Prep vegetables", "Marinate protein", "Thaw freezer items",
            "Check pantry stock", "Plan portion sizes", "Review recipes",
        };

        foreach (var title in prepTitles)
        {
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new { id = Guid.NewGuid(), title, status = "OPEN", priority = "MEDIUM", spaceId },
            };

            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateTaskItem", GraphQLOperations.CreateTaskItem, variables,
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

            var taskId = result.GetProperty("createTaskItem").GetProperty("taskItem").GetProperty("id").GetGuid();
            ctx.TaskItemIds.Add(taskId);
        }
    }
}
