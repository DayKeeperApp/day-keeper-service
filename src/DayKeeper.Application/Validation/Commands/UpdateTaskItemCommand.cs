using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing task item.</summary>
public sealed record UpdateTaskItemCommand(
    Guid Id,
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    TaskItemPriority? Priority,
    Guid? ProjectId,
    DateTime? DueAt,
    DateOnly? DueDate,
    string? RecurrenceRule);
