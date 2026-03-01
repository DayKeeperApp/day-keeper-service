namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing important date.</summary>
public sealed record UpdateImportantDateCommand(
    Guid Id,
    string? Label,
    DateOnly? DateValue,
    Guid? EventTypeId);
