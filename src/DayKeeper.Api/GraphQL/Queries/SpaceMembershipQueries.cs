using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="SpaceMembership"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class SpaceMembershipQueries
{
    /// <summary>Paginated list of space memberships.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<SpaceMembership> GetSpaceMemberships(DayKeeperDbContext dbContext)
    {
        return dbContext.Set<SpaceMembership>().OrderBy(sm => sm.SpaceId);
    }
}
