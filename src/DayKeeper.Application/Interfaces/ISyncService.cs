using DayKeeper.Application.DTOs.Sync;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for incremental sync operations.
/// Provides pull (server-to-client) and push (client-to-server) capabilities
/// using the append-only <see cref="Domain.Entities.ChangeLog"/> as a monotonic cursor.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Retrieves change log entries after the specified cursor for the current tenant,
    /// optionally filtered by space.
    /// </summary>
    Task<SyncPullResponse> PullAsync(
        long? cursor,
        Guid? spaceId,
        int? limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes client-submitted changes using last-writer-wins conflict resolution.
    /// </summary>
    Task<SyncPushResponse> PushAsync(
        IReadOnlyList<SyncPushEntry> changes,
        CancellationToken cancellationToken = default);
}
