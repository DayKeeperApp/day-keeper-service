using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class TaskManagerPersona : IPersona
{
    public string Name => "TaskManager";

    public async Task SeedAsync(PersonaContext ctx, CancellationToken ct)
    {
        var projectCount = ctx.DataFactory.RandomInt(3, 3);
        for (var i = 0; i < projectCount; i++)
        {
            await SeedProjectAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        var roll = ctx.DataFactory.RandomInt(0, 99);
        try
        {
            if (roll < 30)
            {
                await CreateTaskItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 55)
            {
                await UpdateTaskItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 70)
            {
                await CompleteTaskItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 78)
            {
                await CreateProjectAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 88)
            {
                await QueryTaskItemsAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 95)
            {
                await DeleteTaskItemAsync(ctx, ct).ConfigureAwait(false);
            }
            else
            {
                await AssignCategoryAsync(ctx, ct).ConfigureAwait(false);
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

    private static async Task SeedProjectAsync(PersonaContext ctx, CancellationToken ct)
    {
        var projectId = await CreateProjectAsync(ctx, ct).ConfigureAwait(false);
        if (projectId == Guid.Empty)
        {
            return;
        }

        var taskCount = ctx.DataFactory.RandomInt(15, 25);
        for (var j = 0; j < taskCount; j++)
        {
            await CreateTaskItemWithProjectAsync(ctx, projectId, ct).ConfigureAwait(false);
        }
    }

    private static async Task<Guid> CreateProjectAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            var spaceId = ctx.GetWorkingSpaceId();
            var (name, description) = ctx.DataFactory.GenerateProject();
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateProject",
                GraphQLOperations.CreateProject,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, name, description } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createProject").GetProperty("project").GetProperty("id").GetGuid();
            ctx.ProjectIds.Add(id);
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

    private static async Task CreateTaskItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        var projectId = ctx.ProjectIds.IsEmpty ? (Guid?)null : ctx.DataFactory.PickRandom([.. ctx.ProjectIds]);
        var id = await CreateTaskItemCoreAsync(ctx, spaceId, projectId, ct).ConfigureAwait(false);
        if (id != Guid.Empty && ctx.IsWorkingInSharedSpace(spaceId))
        {
            ctx.Coordinator.AddSharedTaskItemId(id);
        }
    }

    private static async Task CreateTaskItemWithProjectAsync(PersonaContext ctx, Guid projectId, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        var id = await CreateTaskItemCoreAsync(ctx, spaceId, projectId, ct).ConfigureAwait(false);
        if (id != Guid.Empty && ctx.IsWorkingInSharedSpace(spaceId))
        {
            ctx.Coordinator.AddSharedTaskItemId(id);
        }
    }

    private static async Task<Guid> CreateTaskItemCoreAsync(PersonaContext ctx, Guid spaceId, Guid? projectId, CancellationToken ct)
    {
        var (title, description, status, priority, dueAt, dueDate) = ctx.DataFactory.GenerateTaskItem();
        var gqlStatus = ToGraphQLStatus(status);
        var gqlPriority = ToGraphQLPriority(priority);
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateTaskItem",
            GraphQLOperations.CreateTaskItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, projectId, title, description, status = gqlStatus, priority = gqlPriority, dueAt, dueDate } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createTaskItem").GetProperty("taskItem").GetProperty("id").GetGuid();
        ctx.TaskItemIds.Add(id);
        return id;
    }

    private static async Task UpdateTaskItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.TaskItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]);
        var (_, _, status, priority, _, _) = ctx.DataFactory.GenerateTaskItem();
        var gqlStatus = ToGraphQLStatus(status);
        var gqlPriority = ToGraphQLPriority(priority);
        await ctx.ApiClient.GraphQLAsync(
            "UpdateTaskItem",
            GraphQLOperations.UpdateTaskItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, status = gqlStatus, priority = gqlPriority } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task CompleteTaskItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.TaskItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]);
        await ctx.ApiClient.GraphQLAsync(
            "CompleteTaskItem",
            GraphQLOperations.CompleteTaskItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task QueryTaskItemsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        await ctx.ApiClient.GraphQLAsync(
            "GetTaskItems",
            GraphQLOperations.GetTaskItems,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeleteTaskItemAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.TaskItemIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeleteTaskItem",
            GraphQLOperations.DeleteTaskItem,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task AssignCategoryAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.TaskItemIds.IsEmpty)
        {
            return;
        }

        var taskItemId = ctx.DataFactory.PickRandom([.. ctx.TaskItemIds]);
        var categoryId = ctx.Coordinator.GetRandomCategoryId();
        if (categoryId is null)
        {
            return;
        }

        await ctx.ApiClient.GraphQLAsync(
            "AssignCategory",
            GraphQLOperations.AssignCategory,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { taskItemId, categoryId } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static string ToGraphQLStatus(string status) => status;

    private static string ToGraphQLPriority(string priority) => priority;
}
