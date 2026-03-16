namespace DayKeeper.UserEmulator.Personas;

public interface IPersona
{
    string Name { get; }
    Task RunIterationAsync(PersonaContext ctx, CancellationToken ct);
    Task SeedAsync(PersonaContext ctx, CancellationToken ct);
}
