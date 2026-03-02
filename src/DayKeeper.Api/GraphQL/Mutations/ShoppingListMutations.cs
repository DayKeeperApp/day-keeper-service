using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="ShoppingList"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class ShoppingListMutations
{
    /// <summary>Creates a new shopping list within a space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateShoppingListNameException>]
    public Task<ShoppingList> CreateShoppingListAsync(
        Guid spaceId,
        string name,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.CreateShoppingListAsync(
            spaceId, name, cancellationToken);
    }

    /// <summary>Updates an existing shopping list.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateShoppingListNameException>]
    public Task<ShoppingList> UpdateShoppingListAsync(
        Guid id,
        string? name,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.UpdateShoppingListAsync(
            id, name, cancellationToken);
    }

    /// <summary>Soft-deletes a shopping list and all associated list items.</summary>
    public Task<bool> DeleteShoppingListAsync(
        Guid id,
        IShoppingListService shoppingListService,
        CancellationToken cancellationToken)
    {
        return shoppingListService.DeleteShoppingListAsync(id, cancellationToken);
    }
}
