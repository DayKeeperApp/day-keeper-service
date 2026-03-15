using DayKeeper.UserEmulator.Client;
using DayKeeper.UserEmulator.Personas;

namespace DayKeeper.UserEmulator.Scenarios;

public sealed class SprintPlanScenario : IScenarioPack
{
    private static readonly string[] SprintCeremonies =
    [
        "Sprint Planning", "Daily Standup", "Sprint Review", "Sprint Retrospective", "Backlog Refinement",
    ];

    public string Name => "SprintPlan";

    public async Task<int> ExecuteAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var projectId = await CreateProjectAsync(ctx, spaceId, ct).ConfigureAwait(false);
        var calendarId = await CreateSprintCalendarAsync(ctx, spaceId, ct).ConfigureAwait(false);

        var taskCount = ctx.DataFactory.RandomInt(15, 25);
        await CreateSprintTasksAsync(ctx, spaceId, projectId, taskCount, ct).ConfigureAwait(false);
        await CreateCeremonyEventsAsync(ctx, calendarId, ct).ConfigureAwait(false);

        return 1 + 1 + taskCount + SprintCeremonies.Length;
    }

    private static async Task<Guid> CreateProjectAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var (name, description) = ctx.DataFactory.GenerateProject();
        var sprintName = $"Sprint: {name}";
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { id = Guid.NewGuid(), name = sprintName, description, spaceId },
        };

        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateProject", GraphQLOperations.CreateProject, variables,
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

        var projectId = result.GetProperty("createProject").GetProperty("project").GetProperty("id").GetGuid();
        ctx.ProjectIds.Add(projectId);
        return projectId;
    }

    private static async Task<Guid> CreateSprintCalendarAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct)
    {
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["input"] = new { id = Guid.NewGuid(), name = "Sprint Ceremonies", color = "#6366F1", spaceId, isDefault = false },
        };

        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateCalendar", GraphQLOperations.CreateCalendar, variables,
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

        var calendarId = result.GetProperty("createCalendar").GetProperty("calendar").GetProperty("id").GetGuid();
        ctx.CalendarIds.Add(calendarId);
        return calendarId;
    }

    private static async Task CreateSprintTasksAsync(
        PersonaContext ctx, Guid spaceId, Guid projectId, int count, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            var (title, _, status, priority, dueAt, dueDate) = ctx.DataFactory.GenerateTaskItem();
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new { id = Guid.NewGuid(), title, status, priority, spaceId, projectId, dueAt, dueDate },
            };

            var result = await ctx.ApiClient.GraphQLAsync(
                "CreateTaskItem", GraphQLOperations.CreateTaskItem, variables,
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);

            var taskId = result.GetProperty("createTaskItem").GetProperty("taskItem").GetProperty("id").GetGuid();
            ctx.TaskItemIds.Add(taskId);
        }
    }

    private static async Task CreateCeremonyEventsAsync(PersonaContext ctx, Guid calendarId, CancellationToken ct)
    {
        var baseDate = DateTime.UtcNow.Date;

        for (var i = 0; i < SprintCeremonies.Length; i++)
        {
            var startAt = baseDate.AddDays(i).AddHours(10);
            var endAt = startAt.AddHours(1);
            var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["input"] = new
                {
                    id = Guid.NewGuid(),
                    title = SprintCeremonies[i],
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
}
