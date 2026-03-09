using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="ShoppingList"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class ShoppingListQueries
{
    /// <summary>Paginated list of shopping lists, optionally filtered by space.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ShoppingList> GetShoppingLists(DayKeeperDbContext dbContext, Guid? spaceId)
    {
        var query = dbContext.Set<ShoppingList>().AsQueryable();

        if (spaceId.HasValue)
        {
            query = query.Where(sl => sl.SpaceId == spaceId.Value);
        }

        return query.OrderBy(sl => sl.Name);
    }

    /// <summary>Retrieves a single shopping list by its unique identifier.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<ShoppingList> GetShoppingListById(Guid id, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<ShoppingList>().Where(sl => sl.Id == id);
    }
}
