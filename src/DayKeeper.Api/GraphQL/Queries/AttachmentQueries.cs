using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Attachment"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class AttachmentQueries
{
    /// <summary>Paginated list of attachments, optionally filtered by parent entity.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Attachment> GetAttachments(
        DayKeeperDbContext dbContext,
        Guid? calendarEventId,
        Guid? taskItemId,
        Guid? personId)
    {
        var query = dbContext.Set<Attachment>().AsQueryable();

        if (calendarEventId.HasValue)
        {
            query = query.Where(a => a.CalendarEventId == calendarEventId.Value);
        }

        if (taskItemId.HasValue)
        {
            query = query.Where(a => a.TaskItemId == taskItemId.Value);
        }

        if (personId.HasValue)
        {
            query = query.Where(a => a.PersonId == personId.Value);
        }

        return query.OrderByDescending(a => a.CreatedAt);
    }

    /// <summary>Retrieves a single attachment by its unique identifier.</summary>
    public Task<Attachment?> GetAttachmentById(
        Guid id,
        IAttachmentService attachmentService,
        CancellationToken cancellationToken)
    {
        return attachmentService.GetAttachmentAsync(id, cancellationToken);
    }
}
