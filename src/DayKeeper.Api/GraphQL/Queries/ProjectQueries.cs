using DayKeeper.Application.Interfaces;
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
    public Task<Project?> GetProjectById(
        Guid id,
        IProjectService projectService,
        CancellationToken cancellationToken)
    {
        return projectService.GetProjectAsync(id, cancellationToken);
    }
}
