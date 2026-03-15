using DayKeeper.UserEmulator.Personas;
using Spectre.Console;

namespace DayKeeper.UserEmulator.Orchestration;

public sealed class DataSeeder
{
    public static async Task SeedAsync(
        IReadOnlyList<(IPersona Persona, PersonaContext Context)> users,
        CancellationToken ct)
    {
        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new SpinnerColumn())
            .StartAsync(async progressCtx =>
            {
                var tasks = new List<Task>();
                foreach (var (persona, context) in users)
                {
                    var task = progressCtx.AddTask($"[cyan]{context.DisplayName}[/] ({persona.Name})");
                    tasks.Add(SeedUserAsync(persona, context, task, ct));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    private static async Task SeedUserAsync(
        IPersona persona,
        PersonaContext context,
        ProgressTask task,
        CancellationToken ct)
    {
        try
        {
            await persona.SeedAsync(context, ct).ConfigureAwait(false);
            task.Increment(100);
        }
        catch (Exception ex)
        {
            task.Description = $"[red]{context.DisplayName} - FAILED: {ex.Message}[/]";
            task.Increment(100);
        }
    }
}
