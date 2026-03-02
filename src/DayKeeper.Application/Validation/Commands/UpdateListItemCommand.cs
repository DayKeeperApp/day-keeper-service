namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for updating an existing list item.
/// </summary>
public sealed record UpdateListItemCommand(
    Guid Id,
    string? Name,
    decimal? Quantity,
    string? Unit,
    bool? IsChecked,
    int? SortOrder);
