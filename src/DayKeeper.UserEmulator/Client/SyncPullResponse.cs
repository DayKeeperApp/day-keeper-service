namespace DayKeeper.UserEmulator.Client;

public sealed record SyncPullResponse(IReadOnlyList<SyncChangeEntry> Changes, long Cursor, bool HasMore);
