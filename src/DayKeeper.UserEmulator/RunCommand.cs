using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.Orchestration;
using Spectre.Console.Cli;

namespace DayKeeper.UserEmulator;

internal sealed class RunCommand : AsyncCommand<EmulatorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, EmulatorSettings settings)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

        var orchestrator = new EmulatorOrchestrator(settings);
        return await orchestrator.RunAsync(cts.Token).ConfigureAwait(false);
    }
}
