namespace DayKeeper.UserEmulator.Client;

public sealed record SyncConflict(string EntityType, Guid EntityId, string Reason, DateTime? ClientTimestamp, DateTime? ServerTimestamp);
