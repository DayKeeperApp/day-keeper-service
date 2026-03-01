namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new person.</summary>
public sealed record CreatePersonCommand(
    Guid SpaceId,
    string FirstName,
    string LastName,
    string? Notes);
