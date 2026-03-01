namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new important date.</summary>
public sealed record CreateImportantDateCommand(
    Guid PersonId,
    string Label,
    DateOnly DateValue,
    Guid? EventTypeId);
