namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new calendar.</summary>
public sealed record CreateCalendarCommand(
    Guid SpaceId,
    string Name,
    string Color,
    bool IsDefault);
