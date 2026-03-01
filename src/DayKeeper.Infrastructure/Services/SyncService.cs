using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using DayKeeper.Application.DTOs.Sync;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Mappings;
using DayKeeper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ISyncService"/>.
/// Provides incremental pull via the <see cref="ChangeLog"/> monotonic cursor
/// and push with last-writer-wins conflict resolution and entity body application.
/// </summary>
public sealed class SyncService(
    DayKeeperDbContext dbContext,
    ITenantContext tenantContext,
    IDateTimeProvider dateTimeProvider,
    ISyncSerializer syncSerializer) : ISyncService
{
    private const int _maxBatchSize = 1000;
    private const int _defaultBatchSize = 1000;

    private readonly DayKeeperDbContext _dbContext = dbContext;
    private readonly ITenantContext _tenantContext = tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly ISyncSerializer _syncSerializer = syncSerializer;

    // Cached MethodInfo for the generic entity-loading helper.
    private static readonly MethodInfo _loadManyMethod =
        typeof(SyncService).GetMethod(
            nameof(LoadEntitiesCoreAsync),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo _loadOneMethod =
        typeof(SyncService).GetMethod(
            nameof(LoadEntityCoreAsync),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> _closedLoadMany = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _closedLoadOne = new();

    /// <inheritdoc />
    public async Task<SyncPullResponse> PullAsync(
        long? cursor,
        Guid? spaceId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveCursor = cursor ?? 0L;
        var effectiveLimit = Math.Clamp(limit ?? _defaultBatchSize, 1, _maxBatchSize);
        var currentTenantId = _tenantContext.CurrentTenantId;

        var query = _dbContext.ChangeLogs
            .Where(cl => cl.Id > effectiveCursor);

        if (currentTenantId.HasValue)
        {
            query = query.Where(cl =>
                cl.TenantId == currentTenantId.Value || cl.TenantId == null);
        }

        if (spaceId.HasValue)
        {
            query = query.Where(cl => cl.SpaceId == spaceId.Value);
        }

        // Fetch limit + 1 to detect whether more changes exist beyond this batch.
        var rawEntries = await query
            .OrderBy(cl => cl.Id)
            .Take(effectiveLimit + 1)
            .Select(cl => new
            {
                cl.Id,
                cl.EntityType,
                cl.EntityId,
                cl.Operation,
                cl.Timestamp,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMore = rawEntries.Count > effectiveLimit;
        if (hasMore)
        {
            rawEntries.RemoveAt(rawEntries.Count - 1);
        }

        var newCursor = rawEntries.Count > 0
            ? rawEntries[^1].Id
            : effectiveCursor;

        var entityData = await LoadEntityDataForPullAsync(rawEntries, cancellationToken)
            .ConfigureAwait(false);

        var entries = rawEntries
            .Select(e => ToSyncChangeEntry(e, entityData))
            .ToList();

        return new SyncPullResponse(entries, newCursor, hasMore);
    }

    private static SyncChangeEntry ToSyncChangeEntry(
        dynamic e,
        Dictionary<(ChangeLogEntityType, Guid), JsonElement> entityData)
    {
        JsonElement? data = e.Operation == ChangeOperation.Deleted
            ? null
            : entityData.TryGetValue(((ChangeLogEntityType)e.EntityType, (Guid)e.EntityId), out var d)
                ? d
                : null;

        return new SyncChangeEntry(e.Id, e.EntityType, e.EntityId, e.Operation, e.Timestamp, data);
    }

    /// <inheritdoc />
    public async Task<SyncPushResponse> PushAsync(
        IReadOnlyList<SyncPushEntry> changes,
        CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0)
        {
            return new SyncPushResponse(0, 0, []);
        }

        var applied = 0;
        var rejected = 0;
        var conflicts = new List<SyncConflict>();
        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var change in changes)
        {
            var latestServerEntry = await _dbContext.ChangeLogs
                .Where(cl => cl.EntityType == change.EntityType
                    && cl.EntityId == change.EntityId)
                .OrderByDescending(cl => cl.Id)
                .Select(cl => new { cl.Timestamp })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            // LWW: client wins when its timestamp >= server's latest, or when
            // the server has no prior record of this entity.
            if (latestServerEntry is not null
                && change.Timestamp < latestServerEntry.Timestamp)
            {
                conflicts.Add(new SyncConflict(
                    change.EntityType,
                    change.EntityId,
                    change.Timestamp,
                    latestServerEntry.Timestamp));
                rejected++;
                continue;
            }

            if (!await ApplyEntityChangeAsync(change, utcNow, cancellationToken)
                    .ConfigureAwait(false))
            {
                continue;
            }

            applied++;
        }

        if (applied > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return new SyncPushResponse(applied, rejected, conflicts);
    }

    // ── Push entity application ──────────────────────────────────────

    /// <summary>
    /// Applies a single push entry to the database.
    /// Returns <c>true</c> if the entity change was applied, <c>false</c> if skipped.
    /// </summary>
    private async Task<bool> ApplyEntityChangeAsync(
        SyncPushEntry change,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!ChangeLogEntityTypeMap.TryGetClrType(change.EntityType, out var clrType))
        {
            return false;
        }

        switch (change.Operation)
        {
            case ChangeOperation.Created:
                {
                    if (!HasData(change.Data))
                    {
                        return false;
                    }

                    var entity = _syncSerializer.Deserialize(change.Data!.Value, change.EntityType);
                    _dbContext.Add(entity);
                    return true;
                }

            case ChangeOperation.Updated:
                {
                    if (!HasData(change.Data))
                    {
                        return false;
                    }

                    var existing = await LoadEntityByIdAsync(clrType, change.EntityId, cancellationToken)
                        .ConfigureAwait(false);

                    if (existing is null)
                    {
                        return false;
                    }

                    var incoming = _syncSerializer.Deserialize(change.Data!.Value, change.EntityType);
                    _dbContext.Entry(existing).CurrentValues.SetValues(incoming);
                    return true;
                }

            case ChangeOperation.Deleted:
                {
                    var existing = await LoadEntityByIdAsync(clrType, change.EntityId, cancellationToken)
                        .ConfigureAwait(false);

                    if (existing is null || existing.IsDeleted)
                    {
                        return true;
                    }

                    existing.DeletedAt = utcNow;
                    return true;
                }

            default:
                return false;
        }
    }

    private static bool HasData(JsonElement? data) =>
        data.HasValue && data.Value.ValueKind != JsonValueKind.Undefined;

    // ── Entity loading helpers ───────────────────────────────────────

    /// <summary>
    /// Batch-loads entities for pull response bodies. Groups by entity type
    /// and loads each group in a single query with <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private async Task<Dictionary<(ChangeLogEntityType, Guid), JsonElement>> LoadEntityDataForPullAsync<T>(
        List<T> rawEntries,
        CancellationToken cancellationToken)
        where T : class
    {
        var result = new Dictionary<(ChangeLogEntityType, Guid), JsonElement>();

        // Use dynamic to access the anonymous type properties.
        var toLoad = rawEntries
            .Select(e => (dynamic)e)
            .Where(e => (ChangeOperation)e.Operation != ChangeOperation.Deleted)
            .GroupBy(e => (ChangeLogEntityType)e.EntityType);

        foreach (var group in toLoad)
        {
            var entityType = group.Key;

            if (!ChangeLogEntityTypeMap.TryGetClrType(entityType, out var clrType))
            {
                continue;
            }

            var ids = group.Select(e => (Guid)e.EntityId).Distinct().ToList();

            var entities = await LoadEntitiesByTypeAsync(clrType, ids, cancellationToken)
                .ConfigureAwait(false);

            foreach (var entity in entities)
            {
                result[(entityType, entity.Id)] = _syncSerializer.Serialize(entity);
            }
        }

        return result;
    }

    private async Task<List<BaseEntity>> LoadEntitiesByTypeAsync(
        Type clrType,
        List<Guid> ids,
        CancellationToken cancellationToken)
    {
        var method = _closedLoadMany.GetOrAdd(clrType, t => _loadManyMethod.MakeGenericMethod(t));
        return await ((Task<List<BaseEntity>>)method.Invoke(this, [ids, cancellationToken])!)
            .ConfigureAwait(false);
    }

    private async Task<BaseEntity?> LoadEntityByIdAsync(
        Type clrType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var method = _closedLoadOne.GetOrAdd(clrType, t => _loadOneMethod.MakeGenericMethod(t));
        return await ((Task<BaseEntity?>)method.Invoke(this, [entityId, cancellationToken])!)
            .ConfigureAwait(false);
    }

    private async Task<List<BaseEntity>> LoadEntitiesCoreAsync<TEntity>(
        List<Guid> ids,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        return await _dbContext.Set<TEntity>()
            .IgnoreQueryFilters()
            .Where(e => ids.Contains(e.Id))
            .Cast<BaseEntity>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<BaseEntity?> LoadEntityCoreAsync<TEntity>(
        Guid entityId,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity
    {
        return await _dbContext.Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken)
            .ConfigureAwait(false);
    }
}
