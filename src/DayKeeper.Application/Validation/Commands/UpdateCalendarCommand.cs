namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing calendar.</summary>
public sealed record UpdateCalendarCommand(
    Guid Id,
    string? Name,
    string? Color,
    bool? IsDefault);
