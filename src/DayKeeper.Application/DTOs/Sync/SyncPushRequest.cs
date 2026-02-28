namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Request body for the push sync endpoint.</summary>
public sealed record SyncPushRequest(
    IReadOnlyList<SyncPushEntry> Changes);
