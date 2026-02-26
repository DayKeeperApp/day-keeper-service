using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Paginated query resolvers for <see cref="Space"/> entities.
/// Demonstrates the cursor-based pagination pattern (Relay Connection specification).
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class SpaceQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Space> GetSpaces(DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Space>().OrderBy(s => s.Name);
    }
}
