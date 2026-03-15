namespace DayKeeper.UserEmulator.Client;

public sealed record SyncPushRequest(IReadOnlyList<SyncPushEntry> Changes);
