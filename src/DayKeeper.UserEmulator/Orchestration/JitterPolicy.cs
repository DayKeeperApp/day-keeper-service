namespace DayKeeper.UserEmulator.Orchestration;

public sealed class JitterPolicy
{
    private readonly int _minDelayMs;
    private readonly int _maxDelayMs;
    private readonly double _burstChance;
    private readonly Random _random;

    public JitterPolicy(int minDelayMs, int maxDelayMs, double burstChance, int? seed = null)
    {
        _minDelayMs = minDelayMs;
        _maxDelayMs = maxDelayMs;
        _burstChance = burstChance;
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    public async Task ApplyJitterAsync(CancellationToken ct)
    {
        var delay = GetNextDelay();
        if (delay > 0)
        {
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
    }

    public bool ShouldBurst() => _random.NextDouble() < _burstChance;

    private int GetNextDelay()
    {
        // Truncated normal distribution via Box-Muller transform.
        // Center between min and max, sigma = range/4 so ~95% of values fall within bounds.
        var mean = (_minDelayMs + _maxDelayMs) / 2.0;
        var sigma = (_maxDelayMs - _minDelayMs) / 4.0;

        var u1 = 1.0 - _random.NextDouble();
        var u2 = 1.0 - _random.NextDouble();
        var normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

        var value = mean + sigma * normal;
        return (int)Math.Clamp(value, _minDelayMs, _maxDelayMs);
    }
}
