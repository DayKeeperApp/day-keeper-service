namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new tenant.</summary>
public sealed record CreateTenantCommand(string Name, string Slug);
