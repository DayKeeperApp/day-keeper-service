using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Project"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class ProjectQueries
{
    /// <summary>Paginated list of projects, optionally filtered by space.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Project> GetProjects(
        DayKeeperDbContext dbContext,
        Guid? spaceId)
    {
        var query = dbContext.Set<Project>().AsQueryable();

        if (spaceId.HasValue)
        {
            query = query.Where(p => p.SpaceId == spaceId.Value);
        }

        return query.OrderBy(p => p.Name);
    }

    /// <summary>Retrieves a single project by its unique identifier.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Project> GetProjectById(Guid id, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Project>().Where(p => p.Id == id);
    }
}
