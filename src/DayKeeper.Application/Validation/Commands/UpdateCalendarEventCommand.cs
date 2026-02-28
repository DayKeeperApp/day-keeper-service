namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing calendar event.</summary>
public sealed record UpdateCalendarEventCommand(
    Guid Id,
    string? Title,
    string? Description,
    bool? IsAllDay,
    DateTime? StartAt,
    DateTime? EndAt,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Timezone,
    string? RecurrenceRule,
    DateTime? RecurrenceEndAt,
    string? Location,
    Guid? EventTypeId);
