using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Person"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class PersonQueries
{
    /// <summary>Paginated list of persons, optionally filtered by space.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Person> GetPersons(DayKeeperDbContext dbContext, Guid? spaceId)
    {
        var query = dbContext.Set<Person>().AsQueryable();

        if (spaceId.HasValue)
        {
            query = query.Where(p => p.SpaceId == spaceId.Value);
        }

        return query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
    }

    /// <summary>Retrieves a single person by its unique identifier.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Person> GetPersonById(Guid id, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Person>().Where(p => p.Id == id);
    }
}
