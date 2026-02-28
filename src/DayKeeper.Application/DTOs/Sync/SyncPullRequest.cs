namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Request body for the pull sync endpoint.</summary>
public sealed record SyncPullRequest(
    long? Cursor,
    Guid? SpaceId,
    int? Limit);
