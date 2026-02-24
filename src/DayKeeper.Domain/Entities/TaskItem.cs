using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A task or to-do item within a <see cref="Space"/>, optionally belonging to a <see cref="Project"/>.
/// Supports priority, status tracking, due dates, and recurrence.
/// </summary>
public class TaskItem : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>
    /// Foreign key to the containing <see cref="Project"/>.
    /// <c>null</c> if the task does not belong to a project.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Short title describing the task.</summary>
    public required string Title { get; set; }

    /// <summary>
    /// Optional longer description providing additional context for the task.
    /// <c>null</c> if no description has been provided.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Current lifecycle status of the task.</summary>
    public TaskItemStatus Status { get; set; }

    /// <summary>Priority level of the task.</summary>
    public TaskItemPriority Priority { get; set; }

    /// <summary>
    /// Optional UTC timestamp indicating when the task is due (date and time).
    /// <c>null</c> if no due date/time is set.
    /// </summary>
    public DateTime? DueAt { get; set; }

    /// <summary>
    /// Optional date-only due date for tasks without a specific time component.
    /// <c>null</c> if no due date is set.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Optional iCalendar RRULE string defining the recurrence pattern (e.g. "FREQ=WEEKLY;BYDAY=MO").
    /// <c>null</c> if the task does not recur.
    /// </summary>
    public string? RecurrenceRule { get; set; }

    /// <summary>
    /// UTC timestamp indicating when the task was completed.
    /// <c>null</c> if the task has not been completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Navigation to the owning space.</summary>
    public Space Space { get; set; } = null!;

    /// <summary>
    /// Navigation to the containing project.
    /// <c>null</c> if the task does not belong to a project.
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>Task-category associations for this task.</summary>
    public ICollection<TaskCategory> TaskCategories { get; set; } = [];
}
