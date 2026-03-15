using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.Personas;

namespace DayKeeper.UserEmulator.Orchestration;

public sealed class UserSimulator
{
    private readonly IPersona _persona;
    private readonly PersonaContext _context;
    private readonly JitterPolicy _jitter;
    private readonly BehaviorArchetype _archetype;
    private readonly ProfileConfig _config;

    public UserSimulator(
        IPersona persona,
        PersonaContext context,
        JitterPolicy jitter,
        BehaviorArchetype archetype,
        ProfileConfig config)
    {
        _persona = persona;
        _context = context;
        _jitter = jitter;
        _archetype = archetype;
        _config = config;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_archetype == BehaviorArchetype.SpikeUser)
                {
                    await RunSpikePatternAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    await _persona.RunIterationAsync(_context, ct).ConfigureAwait(false);
                    await _jitter.ApplyJitterAsync(ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { /* already recorded in metrics, continue */ }
        }
    }

    private async Task RunSpikePatternAsync(CancellationToken ct)
    {
        await RunDormantPhaseAsync(ct).ConfigureAwait(false);
        await RunBurstPhaseAsync(ct).ConfigureAwait(false);
    }

    private async Task RunDormantPhaseAsync(CancellationToken ct)
    {
        var dormant = _context.DataFactory.RandomInt(_config.SpikeDormantMinMs, _config.SpikeDormantMaxMs);
        await Task.Delay(dormant, ct).ConfigureAwait(false);
    }

    private async Task RunBurstPhaseAsync(CancellationToken ct)
    {
        var burstSize = _context.DataFactory.RandomInt(_config.SpikeBurstMinSize, _config.SpikeBurstMaxSize);
        for (var i = 0; i < burstSize && !ct.IsCancellationRequested; i++)
        {
            try
            {
                await _persona.RunIterationAsync(_context, ct).ConfigureAwait(false);
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { /* continue */ }
        }
    }
}
