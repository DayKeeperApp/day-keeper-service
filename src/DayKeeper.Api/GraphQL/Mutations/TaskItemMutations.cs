using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="TaskItem"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class TaskItemMutations
{
    /// <summary>Creates a new task item within a space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<BusinessRuleViolationException>]
    public Task<TaskItem> CreateTaskItemAsync(
        Guid spaceId,
        string title,
        string? description,
        Guid? projectId,
        TaskItemStatus status,
        TaskItemPriority priority,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.CreateTaskItemAsync(
            spaceId, title, description, projectId, status, priority,
            dueAt, dueDate, recurrenceRule, cancellationToken);
    }

    /// <summary>Updates an existing task item.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<BusinessRuleViolationException>]
    public Task<TaskItem> UpdateTaskItemAsync(
        Guid id,
        string? title,
        string? description,
        TaskItemStatus? status,
        TaskItemPriority? priority,
        Guid? projectId,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.UpdateTaskItemAsync(
            id, title, description, status, priority, projectId,
            dueAt, dueDate, recurrenceRule, cancellationToken);
    }

    /// <summary>Marks a task item as completed.</summary>
    [Error<EntityNotFoundException>]
    public Task<TaskItem> CompleteTaskItemAsync(
        Guid id,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.CompleteTaskItemAsync(id, cancellationToken);
    }

    /// <summary>Soft-deletes a task item.</summary>
    public Task<bool> DeleteTaskItemAsync(
        Guid id,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.DeleteTaskItemAsync(id, cancellationToken);
    }

    /// <summary>Assigns a category to a task item.</summary>
    [Error<EntityNotFoundException>]
    [Error<BusinessRuleViolationException>]
    public Task<TaskCategory> AssignCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.AssignCategoryAsync(
            taskItemId, categoryId, cancellationToken);
    }

    /// <summary>Removes a category from a task item.</summary>
    [Error<EntityNotFoundException>]
    public Task<bool> RemoveCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.RemoveCategoryAsync(
            taskItemId, categoryId, cancellationToken);
    }
}
