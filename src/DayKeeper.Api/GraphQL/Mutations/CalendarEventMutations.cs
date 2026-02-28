using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="CalendarEvent"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class CalendarEventMutations
{
    /// <summary>Creates a new event within a calendar.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<CalendarEvent> CreateCalendarEventAsync(
        Guid calendarId,
        string title,
        string? description,
        bool isAllDay,
        DateTime startAt,
        DateTime endAt,
        DateOnly? startDate,
        DateOnly? endDate,
        string timezone,
        string? recurrenceRule,
        DateTime? recurrenceEndAt,
        string? location,
        Guid? eventTypeId,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        return eventService.CreateEventAsync(
            calendarId, title, description, isAllDay,
            startAt, endAt, startDate, endDate, timezone,
            recurrenceRule, recurrenceEndAt, location, eventTypeId,
            cancellationToken);
    }

    /// <summary>Updates an existing calendar event.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<CalendarEvent> UpdateCalendarEventAsync(
        Guid id,
        string? title,
        string? description,
        bool? isAllDay,
        DateTime? startAt,
        DateTime? endAt,
        DateOnly? startDate,
        DateOnly? endDate,
        string? timezone,
        string? recurrenceRule,
        DateTime? recurrenceEndAt,
        string? location,
        Guid? eventTypeId,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        return eventService.UpdateEventAsync(
            id, title, description, isAllDay,
            startAt, endAt, startDate, endDate, timezone,
            recurrenceRule, recurrenceEndAt, location, eventTypeId,
            cancellationToken);
    }

    /// <summary>Soft-deletes a calendar event.</summary>
    public Task<bool> DeleteCalendarEventAsync(
        Guid id,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        return eventService.DeleteEventAsync(id, cancellationToken);
    }
}
