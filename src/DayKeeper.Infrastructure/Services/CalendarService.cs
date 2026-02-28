using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Application service for managing <see cref="Calendar"/> entities.
/// Handles creation, retrieval, update, and soft-delete with duplicate-name
/// checks and mutual exclusion of the default calendar per space.
/// </summary>
public sealed class CalendarService(
    IRepository<Calendar> calendarRepository,
    IRepository<Space> spaceRepository,
    DbContext dbContext) : ICalendarService
{
    private readonly IRepository<Calendar> _calendarRepository = calendarRepository;
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly DbContext _dbContext = dbContext;

    public async Task<Calendar> CreateCalendarAsync(
        Guid spaceId,
        string name,
        string color,
        bool isDefault,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var normalizedName = name.Trim().ToLowerInvariant();

        var nameExists = await _dbContext.Set<Calendar>()
            .AnyAsync(c => c.SpaceId == spaceId && c.NormalizedName == normalizedName, cancellationToken)
            .ConfigureAwait(false);

        if (nameExists) throw new DuplicateCalendarNameException(spaceId, normalizedName);

        if (isDefault)
        {
            await UnsetDefaultCalendarAsync(spaceId, excludeCalendarId: null, cancellationToken)
                .ConfigureAwait(false);
        }

        var calendar = new Calendar
        {
            SpaceId = spaceId,
            Name = name.Trim(),
            NormalizedName = normalizedName,
            Color = color,
            IsDefault = isDefault,
        };

        return await _calendarRepository.AddAsync(calendar, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Calendar?> GetCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
        => await _calendarRepository.GetByIdAsync(calendarId, cancellationToken).ConfigureAwait(false);

    public async Task<Calendar> UpdateCalendarAsync(
        Guid calendarId,
        string? name,
        string? color,
        bool? isDefault,
        CancellationToken cancellationToken = default)
    {
        var calendar = await _calendarRepository.GetByIdAsync(calendarId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Calendar), calendarId);

        if (name is not null)
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            if (!string.Equals(normalizedName, calendar.NormalizedName, StringComparison.Ordinal))
            {
                var nameExists = await _dbContext.Set<Calendar>()
                    .AnyAsync(c => c.SpaceId == calendar.SpaceId
                                && c.NormalizedName == normalizedName
                                && c.Id != calendarId, cancellationToken)
                    .ConfigureAwait(false);

                if (nameExists) throw new DuplicateCalendarNameException(calendar.SpaceId, normalizedName);
            }
            calendar.Name = name.Trim();
            calendar.NormalizedName = normalizedName;
        }

        if (color is not null) calendar.Color = color;

        if (isDefault is true && !calendar.IsDefault)
        {
            await UnsetDefaultCalendarAsync(calendar.SpaceId, excludeCalendarId: calendarId, cancellationToken)
                .ConfigureAwait(false);
            calendar.IsDefault = true;
        }
        else if (isDefault is false)
        {
            calendar.IsDefault = false;
        }

        await _calendarRepository.UpdateAsync(calendar, cancellationToken).ConfigureAwait(false);
        return calendar;
    }

    public async Task<bool> DeleteCalendarAsync(
        Guid calendarId,
        CancellationToken cancellationToken = default)
        => await _calendarRepository.DeleteAsync(calendarId, cancellationToken).ConfigureAwait(false);

    private async Task UnsetDefaultCalendarAsync(
        Guid spaceId,
        Guid? excludeCalendarId,
        CancellationToken cancellationToken)
    {
        var currentDefault = await _dbContext.Set<Calendar>()
            .FirstOrDefaultAsync(c => c.SpaceId == spaceId
                && c.IsDefault
                && (excludeCalendarId == null || c.Id != excludeCalendarId),
                cancellationToken)
            .ConfigureAwait(false);

        if (currentDefault is not null)
        {
            currentDefault.IsDefault = false;
            await _calendarRepository.UpdateAsync(currentDefault, cancellationToken).ConfigureAwait(false);
        }
    }
}
