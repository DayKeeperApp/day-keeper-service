using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Calendar"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class CalendarQueries
{
    /// <summary>Paginated list of calendars, optionally filtered by space.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Calendar> GetCalendars(
        DayKeeperDbContext dbContext,
        Guid? spaceId)
    {
        var query = dbContext.Set<Calendar>().AsQueryable();

        if (spaceId.HasValue)
        {
            query = query.Where(c => c.SpaceId == spaceId.Value);
        }

        return query.OrderBy(c => c.Name);
    }

    /// <summary>Retrieves a single calendar by its unique identifier.</summary>
    public Task<Calendar?> GetCalendarById(
        Guid id,
        ICalendarService calendarService,
        CancellationToken cancellationToken)
    {
        return calendarService.GetCalendarAsync(id, cancellationToken);
    }
}
