using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

public sealed class EventService(
    IRepository<CalendarEvent> eventRepository,
    IRepository<Calendar> calendarRepository,
    IRepository<EventType> eventTypeRepository,
    IRecurrenceExpander recurrenceExpander,
    DbContext dbContext) : IEventService
{
    private readonly IRepository<CalendarEvent> _eventRepository = eventRepository;
    private readonly IRepository<Calendar> _calendarRepository = calendarRepository;
    private readonly IRepository<EventType> _eventTypeRepository = eventTypeRepository;
    private readonly IRecurrenceExpander _recurrenceExpander = recurrenceExpander;
    private readonly DbContext _dbContext = dbContext;

    public async Task<CalendarEvent> CreateEventAsync(
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
        CancellationToken cancellationToken = default)
    {
        _ = await _calendarRepository.GetByIdAsync(calendarId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Calendar), calendarId);

        if (eventTypeId.HasValue)
        {
            _ = await _eventTypeRepository.GetByIdAsync(eventTypeId.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(EventType), eventTypeId.Value);
        }

        var calendarEvent = new CalendarEvent
        {
            CalendarId = calendarId,
            Title = title,
            Description = description,
            IsAllDay = isAllDay,
            StartAt = startAt,
            EndAt = endAt,
            StartDate = startDate,
            EndDate = endDate,
            Timezone = timezone,
            RecurrenceRule = recurrenceRule,
            RecurrenceEndAt = recurrenceEndAt,
            Location = location,
            EventTypeId = eventTypeId,
        };

        return await _eventRepository.AddAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CalendarEvent> UpdateEventAsync(
        Guid eventId,
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
        CancellationToken cancellationToken = default)
    {
        var calendarEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken).ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(CalendarEvent), eventId);

        ApplyScalarUpdates(calendarEvent, title, description, isAllDay, startAt, endAt,
            startDate, endDate, timezone, recurrenceRule, recurrenceEndAt, location);

        await ApplyEventTypeUpdateAsync(calendarEvent, eventTypeId, cancellationToken).ConfigureAwait(false);

        await _eventRepository.UpdateAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
        return calendarEvent;
    }

    public async Task<bool> DeleteEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
        => await _eventRepository.DeleteAsync(eventId, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<ExpandedOccurrence>> GetEventsForRangeAsync(
        IEnumerable<Guid> calendarIds,
        DateTime rangeStart,
        DateTime rangeEnd,
        string timezone,
        CancellationToken cancellationToken = default)
    {
        var calendarIdList = calendarIds.ToList();
        var occurrences = new List<ExpandedOccurrence>();

        // Phase 1: Non-recurring events overlapping the range
        var singleEvents = await _dbContext.Set<CalendarEvent>()
            .Where(e => calendarIdList.Contains(e.CalendarId)
                && e.RecurrenceRule == null
                && e.StartAt < rangeEnd
                && e.EndAt > rangeStart)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var evt in singleEvents)
        {
            occurrences.Add(MapToOccurrence(evt));
        }

        // Phase 2: Recurring event masters
        var recurringEvents = await _dbContext.Set<CalendarEvent>()
            .Where(e => calendarIdList.Contains(e.CalendarId)
                && e.RecurrenceRule != null
                && (e.RecurrenceEndAt == null || e.RecurrenceEndAt >= rangeStart))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (recurringEvents.Count == 0)
        {
            occurrences.Sort(CompareByStartAt);
            return occurrences;
        }

        // Phase 3: Batch-load recurrence exceptions for all recurring events
        var recurringEventIds = recurringEvents.Select(e => e.Id).ToList();

        var exceptions = await _dbContext.Set<RecurrenceException>()
            .Where(ex => recurringEventIds.Contains(ex.CalendarEventId))
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exceptionsByEventId = exceptions
            .GroupBy(ex => ex.CalendarEventId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Phase 4: Expand each recurring event and apply exceptions
        foreach (var master in recurringEvents)
        {
            ExpandRecurringEvent(master, exceptionsByEventId, timezone, rangeStart, rangeEnd, occurrences);
        }

        occurrences.Sort(CompareByStartAt);
        return occurrences;
    }

    private void ExpandRecurringEvent(
        CalendarEvent master,
        Dictionary<Guid, List<RecurrenceException>> exceptionsByEventId,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd,
        List<ExpandedOccurrence> occurrences)
    {
        var duration = master.EndAt - master.StartAt;

        var timestamps = _recurrenceExpander.GetOccurrences(
            master.RecurrenceRule!, master.StartAt, timezone, rangeStart, rangeEnd);

        var eventExceptions = exceptionsByEventId.TryGetValue(master.Id, out var exList)
            ? exList.ToDictionary(ex => ex.OriginalStartAt)
            : [];

        foreach (var occurrenceStart in timestamps)
        {
            if (eventExceptions.TryGetValue(occurrenceStart, out var exception))
            {
                if (exception.IsCancelled) continue;

                occurrences.Add(MapModifiedOccurrence(master, exception, occurrenceStart, duration));
            }
            else
            {
                occurrences.Add(MapRecurringOccurrence(master, occurrenceStart, duration));
            }
        }
    }

    private static ExpandedOccurrence MapToOccurrence(CalendarEvent evt) => new()
    {
        CalendarEventId = evt.Id,
        OriginalStartAt = evt.StartAt,
        Title = evt.Title,
        Description = evt.Description,
        StartAt = evt.StartAt,
        EndAt = evt.EndAt,
        IsAllDay = evt.IsAllDay,
        StartDate = evt.StartDate,
        EndDate = evt.EndDate,
        Timezone = evt.Timezone,
        Location = evt.Location,
        CalendarId = evt.CalendarId,
        EventTypeId = evt.EventTypeId,
        IsRecurring = false,
        IsException = false,
    };

    private static ExpandedOccurrence MapRecurringOccurrence(
        CalendarEvent master, DateTime occurrenceStart, TimeSpan duration) => new()
        {
            CalendarEventId = master.Id,
            OriginalStartAt = occurrenceStart,
            Title = master.Title,
            Description = master.Description,
            StartAt = occurrenceStart,
            EndAt = occurrenceStart + duration,
            IsAllDay = master.IsAllDay,
            StartDate = master.StartDate,
            EndDate = master.EndDate,
            Timezone = master.Timezone,
            Location = master.Location,
            CalendarId = master.CalendarId,
            EventTypeId = master.EventTypeId,
            IsRecurring = true,
            IsException = false,
        };

    private static ExpandedOccurrence MapModifiedOccurrence(
        CalendarEvent master, RecurrenceException exception,
        DateTime occurrenceStart, TimeSpan duration) => new()
        {
            CalendarEventId = master.Id,
            RecurrenceExceptionId = exception.Id,
            OriginalStartAt = occurrenceStart,
            Title = exception.Title ?? master.Title,
            Description = exception.Description ?? master.Description,
            StartAt = exception.StartAt ?? occurrenceStart,
            EndAt = exception.EndAt ?? (occurrenceStart + duration),
            IsAllDay = master.IsAllDay,
            StartDate = master.StartDate,
            EndDate = master.EndDate,
            Timezone = master.Timezone,
            Location = exception.Location ?? master.Location,
            CalendarId = master.CalendarId,
            EventTypeId = master.EventTypeId,
            IsRecurring = true,
            IsException = true,
        };

    private static void ApplyScalarUpdates(
        CalendarEvent calendarEvent,
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
        string? location)
    {
        if (title is not null) calendarEvent.Title = title;
        if (description is not null) calendarEvent.Description = description;
        if (isAllDay.HasValue) calendarEvent.IsAllDay = isAllDay.Value;
        if (startAt.HasValue) calendarEvent.StartAt = startAt.Value;
        if (endAt.HasValue) calendarEvent.EndAt = endAt.Value;
        if (startDate.HasValue) calendarEvent.StartDate = startDate.Value;
        if (endDate.HasValue) calendarEvent.EndDate = endDate.Value;
        if (timezone is not null) calendarEvent.Timezone = timezone;
        if (recurrenceRule is not null) calendarEvent.RecurrenceRule = recurrenceRule;
        if (recurrenceEndAt.HasValue) calendarEvent.RecurrenceEndAt = recurrenceEndAt.Value;
        if (location is not null) calendarEvent.Location = location;
    }

    private async Task ApplyEventTypeUpdateAsync(
        CalendarEvent calendarEvent,
        Guid? eventTypeId,
        CancellationToken cancellationToken)
    {
        if (!eventTypeId.HasValue) return;

        if (eventTypeId.Value == Guid.Empty)
        {
            calendarEvent.EventTypeId = null;
        }
        else
        {
            _ = await _eventTypeRepository.GetByIdAsync(eventTypeId.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(EventType), eventTypeId.Value);
            calendarEvent.EventTypeId = eventTypeId.Value;
        }
    }

    private static int CompareByStartAt(ExpandedOccurrence a, ExpandedOccurrence b)
        => a.StartAt.CompareTo(b.StartAt);
}
