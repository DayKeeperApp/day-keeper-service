namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing person.</summary>
public sealed record UpdatePersonCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Notes);
