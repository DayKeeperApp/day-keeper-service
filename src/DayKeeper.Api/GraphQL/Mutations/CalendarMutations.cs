using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Calendar"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class CalendarMutations
{
    /// <summary>Creates a new calendar within a space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateCalendarNameException>]
    public Task<Calendar> CreateCalendarAsync(
        Guid spaceId,
        string name,
        string color,
        bool isDefault,
        ICalendarService calendarService,
        CancellationToken cancellationToken)
    {
        return calendarService.CreateCalendarAsync(
            spaceId, name, color, isDefault, cancellationToken);
    }

    /// <summary>Updates an existing calendar.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateCalendarNameException>]
    public Task<Calendar> UpdateCalendarAsync(
        Guid id,
        string? name,
        string? color,
        bool? isDefault,
        ICalendarService calendarService,
        CancellationToken cancellationToken)
    {
        return calendarService.UpdateCalendarAsync(
            id, name, color, isDefault, cancellationToken);
    }

    /// <summary>Soft-deletes a calendar.</summary>
    public Task<bool> DeleteCalendarAsync(
        Guid id,
        ICalendarService calendarService,
        CancellationToken cancellationToken)
    {
        return calendarService.DeleteCalendarAsync(id, cancellationToken);
    }
}
