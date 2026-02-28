using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing task items within a space.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="TaskItem"/> and <see cref="TaskCategory"/> entities.
/// </summary>
public interface ITaskItemService
{
    /// <summary>
    /// Creates a new task item within the specified space.
    /// </summary>
    /// <param name="spaceId">The space under which to create the task.</param>
    /// <param name="title">The title for the task.</param>
    /// <param name="description">Optional description for the task.</param>
    /// <param name="projectId">Optional project to assign the task to. Must belong to the same space.</param>
    /// <param name="status">The initial status of the task.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="dueAt">Optional UTC due date/time.</param>
    /// <param name="dueDate">Optional date-only due date.</param>
    /// <param name="recurrenceRule">Optional RRULE string for recurrence.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created task item.</returns>
    /// <exception cref="EntityNotFoundException">The space or project does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">The project does not belong to the same space.</exception>
    Task<TaskItem> CreateTaskItemAsync(
        Guid spaceId,
        string title,
        string? description,
        Guid? projectId,
        TaskItemStatus status,
        TaskItemPriority priority,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a task item by its unique identifier.
    /// </summary>
    /// <param name="taskItemId">The unique identifier of the task item.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The task item if found; otherwise, <c>null</c>.</returns>
    Task<TaskItem?> GetTaskItemAsync(Guid taskItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates properties of an existing task item. All nullable parameters represent
    /// optional partial updates; <c>null</c> means "leave unchanged".
    /// If status changes to <see cref="TaskItemStatus.Completed"/>, CompletedAt is auto-set.
    /// </summary>
    /// <param name="taskItemId">The unique identifier of the task item to update.</param>
    /// <param name="title">The new title, or <c>null</c> to leave unchanged.</param>
    /// <param name="description">The new description, or <c>null</c> to leave unchanged.</param>
    /// <param name="status">The new status, or <c>null</c> to leave unchanged.</param>
    /// <param name="priority">The new priority, or <c>null</c> to leave unchanged.</param>
    /// <param name="projectId">The new project ID, or <c>null</c> to leave unchanged. Pass <see cref="Guid.Empty"/> to unassign.</param>
    /// <param name="dueAt">The new due date/time, or <c>null</c> to leave unchanged.</param>
    /// <param name="dueDate">The new date-only due date, or <c>null</c> to leave unchanged.</param>
    /// <param name="recurrenceRule">The new RRULE, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated task item.</returns>
    /// <exception cref="EntityNotFoundException">The task item or project does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">The new project does not belong to the same space as the task.</exception>
    Task<TaskItem> UpdateTaskItemAsync(
        Guid taskItemId,
        string? title,
        string? description,
        TaskItemStatus? status,
        TaskItemPriority? priority,
        Guid? projectId,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a task item as completed by setting Status to Completed and CompletedAt to now.
    /// </summary>
    /// <param name="taskItemId">The unique identifier of the task item to complete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The completed task item.</returns>
    /// <exception cref="EntityNotFoundException">The task item does not exist.</exception>
    Task<TaskItem> CompleteTaskItemAsync(Guid taskItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a task item.
    /// </summary>
    /// <param name="taskItemId">The unique identifier of the task item to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the task item was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteTaskItemAsync(Guid taskItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a category to a task item by creating a TaskCategory join record.
    /// Validates that the category's tenant matches the task's space tenant (or is a system category).
    /// </summary>
    /// <param name="taskItemId">The task item to assign the category to.</param>
    /// <param name="categoryId">The category to assign.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The created TaskCategory join entity.</returns>
    /// <exception cref="EntityNotFoundException">The task item or category does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">The category tenant does not match the task's space tenant, or the category is already assigned.</exception>
    Task<TaskCategory> AssignCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a category assignment from a task item.
    /// </summary>
    /// <param name="taskItemId">The task item to remove the category from.</param>
    /// <param name="categoryId">The category to remove.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the assignment was found and removed; <c>false</c> if not found.</returns>
    /// <exception cref="EntityNotFoundException">The task item does not exist.</exception>
    Task<bool> RemoveCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands a recurring task's RRULE into concrete occurrence timestamps within a date range.
    /// Uses DueAt as series start, falling back to DueDate (midnight UTC).
    /// </summary>
    /// <param name="taskItemId">The recurring task item to expand.</param>
    /// <param name="timezone">IANA timezone for DST-aware expansion.</param>
    /// <param name="rangeStart">Inclusive start of the query window (UTC).</param>
    /// <param name="rangeEnd">Exclusive end of the query window (UTC).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Sorted list of UTC timestamps for each occurrence in the range.</returns>
    /// <exception cref="EntityNotFoundException">The task item does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">The task has no recurrence rule, or has neither DueAt nor DueDate set.</exception>
    Task<IReadOnlyList<DateTime>> GetRecurringOccurrencesAsync(
        Guid taskItemId,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default);
}
