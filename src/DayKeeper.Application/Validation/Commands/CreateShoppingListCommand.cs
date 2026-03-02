namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for creating a new shopping list.
/// </summary>
public sealed record CreateShoppingListCommand(
    Guid SpaceId,
    string Name);
