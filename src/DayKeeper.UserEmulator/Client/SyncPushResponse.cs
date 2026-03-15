namespace DayKeeper.UserEmulator.Client;

public sealed record SyncPushResponse(int AppliedCount, int RejectedCount, IReadOnlyList<SyncConflict> Conflicts);
