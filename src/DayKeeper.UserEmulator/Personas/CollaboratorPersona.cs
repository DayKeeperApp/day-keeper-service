using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class CollaboratorPersona : IPersona
{
    private static readonly string[] RoleCycle = ["VIEWER", "EDITOR", "OWNER"];

    public string Name => "Collaborator";

    public Task SeedAsync(PersonaContext ctx, CancellationToken ct) => Task.CompletedTask;

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.SharedSpaceIds.Count == 0)
        {
            return;
        }

        try
        {
            await DispatchOperationAsync(ctx, ctx.DataFactory.RandomInt(0, 99), ct).ConfigureAwait(false);
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

    private static async Task DispatchOperationAsync(PersonaContext ctx, int roll, CancellationToken ct)
    {
        if (roll < 15)
        {
            await QuerySharedSpaceTasksAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 30)
        {
            await QuerySharedSpaceEventsAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 45)
        {
            await CreateTaskInSharedSpaceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 55)
        {
            await CreateEventInSharedSpaceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 63)
        {
            await AddSpaceMemberAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 71)
        {
            await UpdateSpaceMemberRoleAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 81)
        {
            await QuerySpaceMembershipsAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 86)
        {
            await CreateSharedSpaceAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 90)
        {
            await RemoveSpaceMemberAsync(ctx, ct).ConfigureAwait(false);
        }
        else if (roll < 95)
        {
            await CreateShoppingListInSharedSpaceAsync(ctx, ct).ConfigureAwait(false);
        }
        else
        {
            await QuerySharedSpaceListsAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    private static Guid GetSharedSpaceId(PersonaContext ctx) =>
        ctx.DataFactory.PickRandom(ctx.SharedSpaceIds);

    private static async Task QuerySharedSpaceTasksAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        await ctx.ApiClient.GraphQLAsync(
            "GetTaskItems",
            GraphQLOperations.GetTaskItems,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task QuerySharedSpaceEventsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        await ctx.ApiClient.GraphQLAsync(
            "GetCalendars",
            GraphQLOperations.GetCalendars,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task CreateTaskInSharedSpaceAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        var (title, description, status, priority, dueAt, dueDate) = ctx.DataFactory.GenerateTaskItem();
        var gqlStatus = ToGraphQLStatus(status);
        var gqlPriority = ToGraphQLPriority(priority);
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateTaskItem",
            GraphQLOperations.CreateTaskItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, title, description, status = gqlStatus, priority = gqlPriority, dueAt, dueDate } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createTaskItem").GetProperty("taskItem").GetProperty("id").GetGuid();
        ctx.TaskItemIds.Add(id);
        ctx.Coordinator.AddSharedTaskItemId(id);
    }

    private static async Task CreateEventInSharedSpaceAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.CalendarIds.IsEmpty)
        {
            return;
        }

        var calendarId = ctx.DataFactory.PickRandom([.. ctx.CalendarIds]);
        var (title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location)
            = ctx.DataFactory.GenerateCalendarEvent(allowRecurrence: false);
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateCalendarEvent",
            GraphQLOperations.CreateCalendarEvent,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { calendarId, title, description, isAllDay, startAt, endAt, startDate, endDate, timezone, recurrenceRule, location } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createCalendarEvent").GetProperty("calendarEvent").GetProperty("id").GetGuid();
        ctx.CalendarEventIds.Add(id);
        ctx.Coordinator.AddSharedCalendarEventId(id);
    }

    private static async Task AddSpaceMemberAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        var userIds = ctx.Coordinator.GetUserIds();
        if (userIds.Count == 0)
        {
            return;
        }

        var userId = userIds[ctx.DataFactory.RandomInt(0, userIds.Count - 1)];
        await ctx.ApiClient.GraphQLAsync(
            "AddSpaceMember",
            GraphQLOperations.AddSpaceMember,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, userId, role = "EDITOR" } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdateSpaceMemberRoleAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        var userIds = ctx.Coordinator.GetUserIds();
        if (userIds.Count == 0)
        {
            return;
        }

        var userId = userIds[ctx.DataFactory.RandomInt(0, userIds.Count - 1)];
        var role = RoleCycle[ctx.DataFactory.RandomInt(0, RoleCycle.Length - 1)];
        await ctx.ApiClient.GraphQLAsync(
            "UpdateSpaceMemberRole",
            GraphQLOperations.UpdateSpaceMemberRole,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, userId, role } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task QuerySpaceMembershipsAsync(PersonaContext ctx, CancellationToken ct)
    {
        await ctx.ApiClient.GraphQLAsync(
            "GetSpaceMemberships",
            GraphQLOperations.GetSpaceMemberships,
            new Dictionary<string, object?>(StringComparer.Ordinal),
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task CreateSharedSpaceAsync(PersonaContext ctx, CancellationToken ct)
    {
        var name = ctx.DataFactory.GenerateSpaceName();
        await ctx.ApiClient.GraphQLAsync(
            "CreateSpace",
            GraphQLOperations.CreateSpace,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { name, spaceType = "SHARED" } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task RemoveSpaceMemberAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        var userIds = ctx.Coordinator.GetUserIds();
        if (userIds.Count == 0)
        {
            return;
        }

        var userId = userIds[ctx.DataFactory.RandomInt(0, userIds.Count - 1)];
        try
        {
            await ctx.ApiClient.GraphQLAsync(
                "RemoveSpaceMember",
                GraphQLOperations.RemoveSpaceMember,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, userId } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        }
        catch (GraphQLException)
        {
            // catches BusinessRuleViolationException (e.g. removing last owner) gracefully
        }
    }

    private static async Task CreateShoppingListInSharedSpaceAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        var name = ctx.DataFactory.GenerateShoppingListName();
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateShoppingList",
            GraphQLOperations.CreateShoppingList,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, name } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createShoppingList").GetProperty("shoppingList").GetProperty("id").GetGuid();
        ctx.ShoppingListIds.Add(id);
        ctx.Coordinator.AddSharedShoppingListId(id);
    }

    private static async Task QuerySharedSpaceListsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = GetSharedSpaceId(ctx);
        await ctx.ApiClient.GraphQLAsync(
            "GetShoppingLists",
            GraphQLOperations.GetShoppingLists,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static string ToGraphQLStatus(string status) => status;

    private static string ToGraphQLPriority(string priority) => priority;
}
