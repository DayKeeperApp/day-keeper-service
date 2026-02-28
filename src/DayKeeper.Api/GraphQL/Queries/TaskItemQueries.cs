using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="TaskItem"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class TaskItemQueries
{
    /// <summary>Paginated list of task items, optionally filtered by space.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TaskItem> GetTaskItems(
        DayKeeperDbContext dbContext,
        Guid? spaceId)
    {
        var query = dbContext.Set<TaskItem>().AsQueryable();

        if (spaceId.HasValue)
        {
            query = query.Where(t => t.SpaceId == spaceId.Value);
        }

        return query.OrderByDescending(t => t.CreatedAt);
    }

    /// <summary>Retrieves a single task item by its unique identifier.</summary>
    public Task<TaskItem?> GetTaskItemById(
        Guid id,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.GetTaskItemAsync(id, cancellationToken);
    }

    /// <summary>Expands a recurring task's recurrence rule into concrete occurrence timestamps.</summary>
    public Task<IReadOnlyList<DateTime>> GetRecurringOccurrences(
        Guid taskItemId,
        string timezone,
        DateTime rangeStart,
        DateTime rangeEnd,
        ITaskItemService taskItemService,
        CancellationToken cancellationToken)
    {
        return taskItemService.GetRecurringOccurrencesAsync(
            taskItemId, timezone, rangeStart, rangeEnd, cancellationToken);
    }
}
