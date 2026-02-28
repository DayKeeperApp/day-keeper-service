namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Response body for the pull sync endpoint.</summary>
public sealed record SyncPullResponse(
    IReadOnlyList<SyncChangeEntry> Changes,
    long Cursor,
    bool HasMore);
