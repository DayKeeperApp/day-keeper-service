using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class ListMakerPersona : IPersona
{
    public string Name => "ListMaker";

    public async Task SeedAsync(PersonaContext ctx, CancellationToken ct)
    {
        var listCount = ctx.DataFactory.RandomInt(3, 5);
        for (var i = 0; i < listCount; i++)
        {
            await SeedShoppingListAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        var roll = ctx.DataFactory.RandomInt(0, 99);
        try
        {
            if (roll < 25)
            {
                await CreateListItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 50)
            {
                await ToggleListItemCheckedAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 60)
            {
                await ReorderListItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 75)
            {
                await GetShoppingListsAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 83)
            {
                await CreateShoppingListAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 90)
            {
                await DeleteListItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else
            {
                await GetListItemsAsync(ctx, ct).ConfigureAwait(false);
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

    private static async Task SeedShoppingListAsync(PersonaContext ctx, CancellationToken ct)
    {
        var listId = await CreateShoppingListAsync(ctx, ct).ConfigureAwait(false);
        if (listId == Guid.Empty)
        {
            return;
        }

        var itemCount = ctx.DataFactory.RandomInt(10, 20);
        for (var j = 0; j < itemCount; j++)
        {
            await CreateListItemForListAsync(ctx, listId, ct).ConfigureAwait(false);
        }
    }

    private static async Task<Guid> CreateShoppingListAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            var spaceId = ctx.GetWorkingSpaceId();
            var name = ctx.DataFactory.GenerateShoppingListName();
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateShoppingList",
                GraphQLOperations.CreateShoppingList,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, name } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createShoppingList").GetProperty("shoppingList").GetProperty("id").GetGuid();
            ctx.ShoppingListIds.Add(id);
            if (ctx.IsWorkingInSharedSpace(spaceId))
            {
                ctx.Coordinator.AddSharedShoppingListId(id);
            }

            return id;
        }
        catch (GraphQLException)
        {
            return Guid.Empty;
        }
        catch (HttpRequestException)
        {
            return Guid.Empty;
        }
    }

    private static async Task CreateListItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ShoppingListIds.IsEmpty)
        {
            return;
        }

        var listId = ctx.DataFactory.PickRandom([.. ctx.ShoppingListIds]);
        await CreateListItemForListAsync(ctx, listId, ct).ConfigureAwait(false);
    }

    private static async Task CreateListItemForListAsync(PersonaContext ctx, Guid shoppingListId, CancellationToken ct)
    {
        try
        {
            var (name, quantity, unit) = ctx.DataFactory.GenerateListItem();
            var sortOrder = ctx.DataFactory.RandomInt(0, 999);
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateListItem",
                GraphQLOperations.CreateListItem,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { shoppingListId, name, quantity, unit, sortOrder } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createListItem").GetProperty("listItem").GetProperty("id").GetGuid();
            ctx.ListItemIds.Add(id);
            ctx.Coordinator.AddSharedListItemId(id);
        }
        catch (Exception ex) when (ex is GraphQLException or HttpRequestException or InvalidOperationException)
        {
            // error already recorded in metrics
        }
    }

    private static async Task ToggleListItemCheckedAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ListItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.ListItemIds]);
        var isChecked = ctx.DataFactory.RandomBool();
        await ctx.ApiClient.GraphQLAsync(
            "UpdateListItem",
            GraphQLOperations.UpdateListItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, isChecked } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task ReorderListItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ListItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.ListItemIds]);
        var sortOrder = ctx.DataFactory.RandomInt(0, 999);
        await ctx.ApiClient.GraphQLAsync(
            "UpdateListItem",
            GraphQLOperations.UpdateListItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, sortOrder } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetShoppingListsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        await ctx.ApiClient.GraphQLAsync(
            "GetShoppingLists",
            GraphQLOperations.GetShoppingLists,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeleteListItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ListItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.ListItemIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeleteListItem",
            GraphQLOperations.DeleteListItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetListItemsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        await ctx.ApiClient.GraphQLAsync(
            "GetShoppingLists",
            GraphQLOperations.GetShoppingLists,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }
}
