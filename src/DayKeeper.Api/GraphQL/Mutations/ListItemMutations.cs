using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="ListItem"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class ListItemMutations
{
    /// <summary>Creates a new list item on a shopping list.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ListItem> CreateListItemAsync(
        Guid shoppingListId,
        string name,
        decimal quantity,
        string? unit,
        int sortOrder,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.CreateListItemAsync(
            shoppingListId, name, quantity, unit, sortOrder, cancellationToken);
    }

    /// <summary>Updates an existing list item.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ListItem> UpdateListItemAsync(
        Guid id,
        string? name,
        decimal? quantity,
        string? unit,
        bool? isChecked,
        int? sortOrder,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.UpdateListItemAsync(
            id, name, quantity, unit, isChecked, sortOrder, cancellationToken);
    }

    /// <summary>Soft-deletes a list item.</summary>
    public Task<bool> DeleteListItemAsync(
        Guid id,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.DeleteListItemAsync(id, cancellationToken);
    }
}
