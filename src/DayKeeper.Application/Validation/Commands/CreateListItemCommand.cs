namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for creating a new list item.
/// </summary>
public sealed record CreateListItemCommand(
    Guid ShoppingListId,
    string Name,
    decimal Quantity,
    string? Unit,
    int SortOrder);
