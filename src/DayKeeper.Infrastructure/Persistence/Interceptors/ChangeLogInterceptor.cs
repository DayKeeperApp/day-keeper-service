using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Interfaces;
using DayKeeper.Domain.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DayKeeper.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Appends <see cref="ChangeLog"/> entries for every tracked <see cref="BaseEntity"/>
/// mutation before changes are persisted. Because <see cref="ChangeLog"/> does not extend
/// <see cref="BaseEntity"/>, the interceptor cannot observe its own writes, preventing
/// infinite recursion.
/// </summary>
public sealed class ChangeLogInterceptor(
    IDateTimeProvider dateTimeProvider,
    ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AppendChangeLogEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AppendChangeLogEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void AppendChangeLogEntries(DbContext context)
    {
        var utcNow = dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
        {
            var operation = entry.State switch
            {
                EntityState.Added => ChangeOperation.Created,
                EntityState.Modified => ChangeOperation.Updated,
                EntityState.Deleted => ChangeOperation.Deleted,
                _ => (ChangeOperation?)null,
            };

            if (operation is null)
            {
                continue;
            }

            if (!ChangeLogEntityTypeMap.TryGetEntityType(entry.Entity.GetType(), out var entityType))
            {
                continue;
            }

            // Soft-delete detection: Modified + DeletedAt changed from null to non-null
            if (entry.State == EntityState.Modified
                && entry.Property(nameof(BaseEntity.DeletedAt)).IsModified
                && entry.Entity.DeletedAt.HasValue)
            {
                operation = ChangeOperation.Deleted;
            }

            context.Set<ChangeLog>().Add(new ChangeLog
            {
                EntityType = entityType,
                EntityId = entry.Entity.Id,
                Operation = operation.Value,
                TenantId = ResolveTenantId(entry.Entity),
                SpaceId = ResolveSpaceId(entry.Entity),
                Timestamp = utcNow,
            });
        }
    }

    private Guid? ResolveTenantId(BaseEntity entity) => entity switch
    {
        ITenantScoped ts => ts.TenantId,
        IOptionalTenantScoped ots => ots.TenantId,
        Tenant t => t.Id,
        _ => tenantContext.CurrentTenantId,
    };

    private static Guid? ResolveSpaceId(BaseEntity entity) => entity switch
    {
        Space s => s.Id,
        SpaceMembership sm => sm.SpaceId,
        Calendar c => c.SpaceId,
        Person p => p.SpaceId,
        Project pr => pr.SpaceId,
        TaskItem ti => ti.SpaceId,
        ShoppingList sl => sl.SpaceId,
        _ => null,
    };
}
