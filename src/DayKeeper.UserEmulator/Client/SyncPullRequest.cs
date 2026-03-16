namespace DayKeeper.UserEmulator.Client;

public sealed record SyncPullRequest(long? Cursor, Guid? SpaceId, int? Limit);
