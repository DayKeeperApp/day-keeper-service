using DayKeeper.UserEmulator.Personas;

namespace DayKeeper.UserEmulator.Scenarios;

public interface IScenarioPack
{
    string Name { get; }
    Task<int> ExecuteAsync(PersonaContext ctx, Guid spaceId, CancellationToken ct);
}
