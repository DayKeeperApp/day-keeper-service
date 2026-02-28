namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Response body for the push sync endpoint.</summary>
public sealed record SyncPushResponse(
    int AppliedCount,
    int RejectedCount,
    IReadOnlyList<SyncConflict> Conflicts);
