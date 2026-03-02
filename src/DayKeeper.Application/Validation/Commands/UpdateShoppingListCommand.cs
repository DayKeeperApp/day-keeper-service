namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for updating an existing shopping list.
/// </summary>
public sealed record UpdateShoppingListCommand(
    Guid Id,
    string? Name);
