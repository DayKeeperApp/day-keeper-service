using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IShoppingListService"/>.
/// Orchestrates business rules for shopping list and list item
/// management using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class ShoppingListService(
    IRepository<ShoppingList> shoppingListRepository,
    IRepository<ListItem> listItemRepository,
    IRepository<Space> spaceRepository,
    DbContext dbContext) : IShoppingListService
{
    private readonly IRepository<ShoppingList> _shoppingListRepository = shoppingListRepository;
    private readonly IRepository<ListItem> _listItemRepository = listItemRepository;
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly DbContext _dbContext = dbContext;

    // ── ShoppingList CRUD ─────────────────────────────────────

    /// <inheritdoc />
    public async Task<ShoppingList> CreateShoppingListAsync(
        Guid spaceId,
        string name,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var normalizedName = NormalizeName(name);

        var nameExists = await _dbContext.Set<ShoppingList>()
            .AnyAsync(sl => sl.SpaceId == spaceId && sl.NormalizedName == normalizedName,
                cancellationToken)
            .ConfigureAwait(false);

        if (nameExists)
        {
            throw new DuplicateShoppingListNameException(spaceId, normalizedName);
        }

        var shoppingList = new ShoppingList
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            NormalizedName = normalizedName,
        };

        return await _shoppingListRepository.AddAsync(shoppingList, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ShoppingList?> GetShoppingListAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default)
    {
        return await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ShoppingList> UpdateShoppingListAsync(
        Guid shoppingListId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ShoppingList), shoppingListId);

        if (name is not null)
        {
            var normalizedName = NormalizeName(name);

            if (!string.Equals(normalizedName, shoppingList.NormalizedName, StringComparison.Ordinal))
            {
                var nameExists = await _dbContext.Set<ShoppingList>()
                    .AnyAsync(sl => sl.SpaceId == shoppingList.SpaceId
                                && sl.NormalizedName == normalizedName
                                && sl.Id != shoppingListId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (nameExists)
                {
                    throw new DuplicateShoppingListNameException(shoppingList.SpaceId, normalizedName);
                }
            }

            shoppingList.Name = name.Trim();
            shoppingList.NormalizedName = normalizedName;
        }

        await _shoppingListRepository.UpdateAsync(shoppingList, cancellationToken)
            .ConfigureAwait(false);

        return shoppingList;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteShoppingListAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default)
    {
        return await _shoppingListRepository.DeleteAsync(shoppingListId, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── ListItem CRUD ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ListItem> CreateListItemAsync(
        Guid shoppingListId,
        string name,
        decimal quantity,
        string? unit,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        _ = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ShoppingList), shoppingListId);

        var listItem = new ListItem
        {
            ShoppingListId = shoppingListId,
            Name = name.Trim(),
            Quantity = quantity,
            Unit = unit?.Trim(),
            IsChecked = false,
            SortOrder = sortOrder,
        };

        return await _listItemRepository.AddAsync(listItem, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ListItem> UpdateListItemAsync(
        Guid id,
        string? name,
        decimal? quantity,
        string? unit,
        bool? isChecked,
        int? sortOrder,
        CancellationToken cancellationToken = default)
    {
        var listItem = await _listItemRepository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ListItem), id);

        if (name is not null)
        {
            listItem.Name = name.Trim();
        }

        if (quantity.HasValue)
        {
            listItem.Quantity = quantity.Value;
        }

        if (unit is not null)
        {
            listItem.Unit = unit.Trim();
        }

        if (isChecked.HasValue)
        {
            listItem.IsChecked = isChecked.Value;
        }

        if (sortOrder.HasValue)
        {
            listItem.SortOrder = sortOrder.Value;
        }

        await _listItemRepository.UpdateAsync(listItem, cancellationToken)
            .ConfigureAwait(false);

        return listItem;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteListItemAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _listItemRepository.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static string NormalizeName(string name)
        => name.Trim().ToLowerInvariant();
}
