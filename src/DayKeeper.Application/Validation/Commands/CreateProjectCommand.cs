namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new project.</summary>
public sealed record CreateProjectCommand(
    Guid SpaceId,
    string Name,
    string? Description);
