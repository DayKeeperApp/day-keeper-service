using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ITaskItemService"/>.
/// Orchestrates business rules for task item management, category assignment,
/// and recurrence expansion using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class TaskItemService(
    IRepository<TaskItem> taskItemRepository,
    IRepository<Space> spaceRepository,
    IRepository<Project> projectRepository,
    IRepository<Category> categoryRepository,
    IRepository<TaskCategory> taskCategoryRepository,
    IDateTimeProvider dateTimeProvider,
    IRecurrenceExpander recurrenceExpander,
    DbContext dbContext) : ITaskItemService
{
    private readonly IRepository<TaskItem> _taskItemRepository = taskItemRepository;
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly IRepository<Project> _projectRepository = projectRepository;
    private readonly IRepository<Category> _categoryRepository = categoryRepository;
    private readonly IRepository<TaskCategory> _taskCategoryRepository = taskCategoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IRecurrenceExpander _recurrenceExpander = recurrenceExpander;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<TaskItem> CreateTaskItemAsync(
        Guid spaceId,
        string title,
        string? description,
        Guid? projectId,
        TaskItemStatus status,
        TaskItemPriority priority,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        if (projectId.HasValue)
        {
            await ValidateProjectBelongsToSpaceAsync(projectId.Value, spaceId, cancellationToken)
                .ConfigureAwait(false);
        }

        var taskItem = new TaskItem
        {
            SpaceId = spaceId,
            Title = title,
            Description = description,
            ProjectId = projectId,
            Status = status,
            Priority = priority,
            DueAt = dueAt,
            DueDate = dueDate,
            RecurrenceRule = recurrenceRule,
        };

        return await _taskItemRepository.AddAsync(taskItem, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TaskItem?> GetTaskItemAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        return await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TaskItem> UpdateTaskItemAsync(
        Guid taskItemId,
        string? title,
        string? description,
        TaskItemStatus? status,
        TaskItemPriority? priority,
        Guid? projectId,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule,
        CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId);

        ApplyScalarUpdates(taskItem, title, description, priority, dueAt, dueDate, recurrenceRule);

        await ApplyProjectUpdateAsync(taskItem, projectId, cancellationToken)
            .ConfigureAwait(false);

        ApplyStatusUpdate(taskItem, status);

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken)
            .ConfigureAwait(false);

        return taskItem;
    }

    /// <inheritdoc />
    public async Task<TaskItem> CompleteTaskItemAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId);

        taskItem.Status = TaskItemStatus.Completed;
        taskItem.CompletedAt = _dateTimeProvider.UtcNow;

        await _taskItemRepository.UpdateAsync(taskItem, cancellationToken)
            .ConfigureAwait(false);

        return taskItem;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTaskItemAsync(
        Guid taskItemId,
        CancellationToken cancellationToken = default)
    {
        return await _taskItemRepository.DeleteAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TaskCategory> AssignCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId);

        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Category), categoryId);

        // Validate tenant match: system categories (null TenantId) are allowed for any space
        if (category.TenantId.HasValue)
        {
            var space = await _spaceRepository.GetByIdAsync(taskItem.SpaceId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new EntityNotFoundException(nameof(Space), taskItem.SpaceId);

            if (category.TenantId.Value != space.TenantId)
            {
                throw new BusinessRuleViolationException(
                    "CategoryTenantMismatch",
                    "The category does not belong to the same tenant as the task's space.");
            }
        }

        var alreadyAssigned = await _dbContext.Set<TaskCategory>()
            .AnyAsync(tc => tc.TaskItemId == taskItemId && tc.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);

        if (alreadyAssigned)
        {
            throw new BusinessRuleViolationException(
                "DuplicateCategoryAssignment",
                "This category is already assigned to the task.");
        }

        var taskCategory = new TaskCategory
        {
            TaskItemId = taskItemId,
            CategoryId = categoryId,
        };

        return await _taskCategoryRepository.AddAsync(taskCategory, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveCategoryAsync(
        Guid taskItemId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        _ = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId);

        var taskCategory = await _dbContext.Set<TaskCategory>()
            .FirstOrDefaultAsync(tc => tc.TaskItemId == taskItemId && tc.CategoryId == categoryId,
                cancellationToken)
            .ConfigureAwait(false);

        if (taskCategory is null)
        {
            return false;
        }

        return await _taskCategoryRepository.DeleteAsync(taskCategory.Id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateTime>> GetRecurringOccurrencesAsync(
        Guid taskItemId,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskItemRepository.GetByIdAsync(taskItemId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(TaskItem), taskItemId);

        if (string.IsNullOrWhiteSpace(taskItem.RecurrenceRule))
        {
            throw new BusinessRuleViolationException(
                "NoRecurrenceRule",
                "The task does not have a recurrence rule.");
        }

        DateTime seriesStart;

        if (taskItem.DueAt.HasValue)
        {
            seriesStart = taskItem.DueAt.Value;
        }
        else if (taskItem.DueDate.HasValue)
        {
            seriesStart = taskItem.DueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }
        else
        {
            throw new BusinessRuleViolationException(
                "NoSeriesStart",
                "The task must have DueAt or DueDate set to expand recurrences.");
        }

        return _recurrenceExpander.GetOccurrences(
            taskItem.RecurrenceRule, seriesStart, timezone, rangeStart, rangeEnd);
    }

    private static void ApplyScalarUpdates(
        TaskItem taskItem,
        string? title,
        string? description,
        TaskItemPriority? priority,
        DateTime? dueAt,
        DateOnly? dueDate,
        string? recurrenceRule)
    {
        if (title is not null) taskItem.Title = title;
        if (description is not null) taskItem.Description = description;
        if (priority.HasValue) taskItem.Priority = priority.Value;
        if (dueAt.HasValue) taskItem.DueAt = dueAt.Value;
        if (dueDate.HasValue) taskItem.DueDate = dueDate.Value;
        if (recurrenceRule is not null) taskItem.RecurrenceRule = recurrenceRule;
    }

    private async Task ApplyProjectUpdateAsync(
        TaskItem taskItem,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        if (!projectId.HasValue) return;

        if (projectId.Value == Guid.Empty)
        {
            taskItem.ProjectId = null;
        }
        else
        {
            await ValidateProjectBelongsToSpaceAsync(projectId.Value, taskItem.SpaceId, cancellationToken)
                .ConfigureAwait(false);

            taskItem.ProjectId = projectId.Value;
        }
    }

    private void ApplyStatusUpdate(TaskItem taskItem, TaskItemStatus? status)
    {
        if (!status.HasValue || status.Value == taskItem.Status) return;

        taskItem.Status = status.Value;
        taskItem.CompletedAt = status.Value == TaskItemStatus.Completed
            ? _dateTimeProvider.UtcNow
            : null;
    }

    private async Task ValidateProjectBelongsToSpaceAsync(
        Guid projectId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (project.SpaceId != spaceId)
        {
            throw new BusinessRuleViolationException(
                "ProjectSpaceMismatch",
                "The project does not belong to the same space as the task.");
        }
    }
}
