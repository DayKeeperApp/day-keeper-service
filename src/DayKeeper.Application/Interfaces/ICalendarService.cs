using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing calendars within a space.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="Calendar"/> entities.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Creates a new calendar within the specified space.
    /// When <paramref name="isDefault"/> is <c>true</c>, any existing default
    /// calendar in the same space is automatically unset.
    /// </summary>
    /// <param name="spaceId">The space under which to create the calendar.</param>
    /// <param name="name">The display name for the calendar.</param>
    /// <param name="color">Hex color code for the calendar.</param>
    /// <param name="isDefault">Whether this calendar should be the default for the space.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created calendar.</returns>
    /// <exception cref="EntityNotFoundException">The specified space does not exist.</exception>
    /// <exception cref="DuplicateCalendarNameException">A calendar with the same normalized name already exists in this space.</exception>
    Task<Calendar> CreateCalendarAsync(
        Guid spaceId,
        string name,
        string color,
        bool isDefault,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a calendar by its unique identifier.
    /// </summary>
    /// <param name="calendarId">The unique identifier of the calendar.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The calendar if found; otherwise, <c>null</c>.</returns>
    Task<Calendar?> GetCalendarAsync(Guid calendarId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates properties of an existing calendar. All nullable parameters represent
    /// optional partial updates; <c>null</c> means "leave unchanged".
    /// When <paramref name="isDefault"/> is set to <c>true</c>, the previous default
    /// calendar in the same space (if any) is automatically unset.
    /// </summary>
    /// <param name="calendarId">The unique identifier of the calendar to update.</param>
    /// <param name="name">The new display name, or <c>null</c> to leave unchanged.</param>
    /// <param name="color">The new hex color code, or <c>null</c> to leave unchanged.</param>
    /// <param name="isDefault">The new default status, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated calendar.</returns>
    /// <exception cref="EntityNotFoundException">The calendar does not exist.</exception>
    /// <exception cref="DuplicateCalendarNameException">The new name conflicts with an existing calendar in the same space.</exception>
    Task<Calendar> UpdateCalendarAsync(
        Guid calendarId,
        string? name,
        string? color,
        bool? isDefault,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a calendar.
    /// </summary>
    /// <param name="calendarId">The unique identifier of the calendar to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the calendar was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteCalendarAsync(Guid calendarId, CancellationToken cancellationToken = default);
}
