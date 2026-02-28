namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing project.</summary>
public sealed record UpdateProjectCommand(
    Guid Id,
    string? Name,
    string? Description);
