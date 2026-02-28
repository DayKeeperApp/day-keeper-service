using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="CalendarEvent"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class CalendarEventQueries
{
    /// <summary>Paginated list of calendar events, optionally filtered by calendar.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<CalendarEvent> GetCalendarEvents(
        DayKeeperDbContext dbContext,
        Guid? calendarId)
    {
        var query = dbContext.Set<CalendarEvent>().AsQueryable();

        if (calendarId.HasValue)
        {
            query = query.Where(e => e.CalendarId == calendarId.Value);
        }

        return query.OrderByDescending(e => e.StartAt);
    }

    /// <summary>Retrieves a single calendar event by its unique identifier.</summary>
    public Task<CalendarEvent?> GetCalendarEventById(
        Guid id,
        DayKeeperDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<CalendarEvent>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all event occurrences within a time range for one or more calendars,
    /// expanding recurring events and applying recurrence exceptions.
    /// </summary>
    public Task<IReadOnlyList<ExpandedOccurrence>> GetEventsForRange(
        Guid[] calendarIds,
        DateTime rangeStart,
        DateTime rangeEnd,
        string timezone,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        return eventService.GetEventsForRangeAsync(
            calendarIds, rangeStart, rangeEnd, timezone, cancellationToken);
    }
}
