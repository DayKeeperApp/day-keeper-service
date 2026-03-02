using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing shopping lists and their associated list items.
/// Orchestrates business rules, validation, and persistence for
/// the Lists aggregate rooted at <see cref="ShoppingList"/>.
/// </summary>
public interface IShoppingListService
{
    // ── ShoppingList CRUD ─────────────────────────────────────

    /// <summary>
    /// Creates a new shopping list within the specified space.
    /// </summary>
    /// <param name="spaceId">The space under which to create the shopping list.</param>
    /// <param name="name">The display name for the shopping list.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created shopping list.</returns>
    /// <exception cref="EntityNotFoundException">The specified space does not exist.</exception>
    /// <exception cref="DuplicateShoppingListNameException">A shopping list with the same normalized name already exists in this space.</exception>
    Task<ShoppingList> CreateShoppingListAsync(
        Guid spaceId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a shopping list by its unique identifier.
    /// </summary>
    /// <param name="shoppingListId">The unique identifier of the shopping list.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The shopping list if found; otherwise, <c>null</c>.</returns>
    Task<ShoppingList?> GetShoppingListAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing shopping list.
    /// Pass <c>null</c> to leave a field unchanged.
    /// </summary>
    /// <param name="shoppingListId">The unique identifier of the shopping list to update.</param>
    /// <param name="name">The new name, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated shopping list.</returns>
    /// <exception cref="EntityNotFoundException">The shopping list does not exist.</exception>
    /// <exception cref="DuplicateShoppingListNameException">The new name conflicts with an existing shopping list in the same space.</exception>
    Task<ShoppingList> UpdateShoppingListAsync(
        Guid shoppingListId,
        string? name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a shopping list and all associated list items.
    /// </summary>
    /// <param name="shoppingListId">The unique identifier of the shopping list to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the shopping list was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteShoppingListAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default);

    // ── ListItem CRUD ─────────────────────────────────────────

    /// <summary>
    /// Creates a new list item on the specified shopping list.
    /// </summary>
    /// <param name="shoppingListId">The shopping list to add the item to.</param>
    /// <param name="name">The item name.</param>
    /// <param name="quantity">The quantity of the item.</param>
    /// <param name="unit">Optional unit of measure.</param>
    /// <param name="sortOrder">Zero-based display order position.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created list item.</returns>
    /// <exception cref="EntityNotFoundException">The specified shopping list does not exist.</exception>
    Task<ListItem> CreateListItemAsync(
        Guid shoppingListId,
        string name,
        decimal quantity,
        string? unit,
        int sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing list item.
    /// Pass <c>null</c> to leave a field unchanged.
    /// </summary>
    /// <param name="id">The unique identifier of the list item to update.</param>
    /// <param name="name">The new name, or <c>null</c> to leave unchanged.</param>
    /// <param name="quantity">The new quantity, or <c>null</c> to leave unchanged.</param>
    /// <param name="unit">The new unit, or <c>null</c> to leave unchanged.</param>
    /// <param name="isChecked">The new checked status, or <c>null</c> to leave unchanged.</param>
    /// <param name="sortOrder">The new sort order, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated list item.</returns>
    /// <exception cref="EntityNotFoundException">The list item does not exist.</exception>
    Task<ListItem> UpdateListItemAsync(
        Guid id,
        string? name,
        decimal? quantity,
        string? unit,
        bool? isChecked,
        int? sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a list item.
    /// </summary>
    /// <param name="id">The unique identifier of the list item to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteListItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
