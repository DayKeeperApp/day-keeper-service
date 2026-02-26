using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Space"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class SpaceQueries
{
    /// <summary>Paginated list of spaces.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Space> GetSpaces(DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Space>().OrderBy(s => s.Name);
    }

    /// <summary>Retrieves a single space by its unique identifier.</summary>
    public Task<Space?> GetSpaceById(
        Guid id,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.GetSpaceAsync(id, cancellationToken);
    }
}
