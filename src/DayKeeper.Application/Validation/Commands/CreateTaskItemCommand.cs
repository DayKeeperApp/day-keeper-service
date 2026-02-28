using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new task item.</summary>
public sealed record CreateTaskItemCommand(
    Guid SpaceId,
    string Title,
    string? Description,
    Guid? ProjectId,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueAt,
    DateOnly? DueDate,
    string? RecurrenceRule);
