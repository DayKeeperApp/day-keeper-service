using DayKeeper.Application.DTOs.Sync;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ISyncService"/>.
/// Provides incremental pull via the <see cref="ChangeLog"/> monotonic cursor
/// and push with last-writer-wins conflict resolution.
/// </summary>
public sealed class SyncService(
    DayKeeperDbContext dbContext,
    ITenantContext tenantContext,
    IDateTimeProvider dateTimeProvider) : ISyncService
{
    private const int MaxBatchSize = 1000;
    private const int DefaultBatchSize = 1000;

    private readonly DayKeeperDbContext _dbContext = dbContext;
    private readonly ITenantContext _tenantContext = tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    /// <inheritdoc />
    public async Task<SyncPullResponse> PullAsync(
        long? cursor,
        Guid? spaceId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveCursor = cursor ?? 0L;
        var effectiveLimit = Math.Clamp(limit ?? DefaultBatchSize, 1, MaxBatchSize);
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
        var entries = await query
            .OrderBy(cl => cl.Id)
            .Take(effectiveLimit + 1)
            .Select(cl => new SyncChangeEntry(
                cl.Id,
                cl.EntityType,
                cl.EntityId,
                cl.Operation,
                cl.Timestamp))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasMore = entries.Count > effectiveLimit;
        if (hasMore)
        {
            entries.RemoveAt(entries.Count - 1);
        }

        var newCursor = entries.Count > 0
            ? entries[^1].Id
            : effectiveCursor;

        return new SyncPullResponse(entries, newCursor, hasMore);
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
        var currentTenantId = _tenantContext.CurrentTenantId;
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

            // Record the accepted change in the ChangeLog.
            // Actual entity body application is deferred to DKS-4kk.
            _dbContext.ChangeLogs.Add(new ChangeLog
            {
                EntityType = change.EntityType,
                EntityId = change.EntityId,
                Operation = change.Operation,
                TenantId = currentTenantId,
                SpaceId = null,
                Timestamp = utcNow,
            });

            applied++;
        }

        if (applied > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return new SyncPushResponse(applied, rejected, conflicts);
    }
}
