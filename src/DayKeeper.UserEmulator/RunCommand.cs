using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.Orchestration;
using Spectre.Console.Cli;

namespace DayKeeper.UserEmulator;

internal sealed class RunCommand : AsyncCommand<EmulatorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, EmulatorSettings settings)
    {
        var orchestrator = new EmulatorOrchestrator(settings);
        return await orchestrator.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
