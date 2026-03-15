using System.Text.Json;

namespace DayKeeper.UserEmulator.Client;

public sealed record SyncChangeEntry(long Id, int EntityType, Guid EntityId, int Operation, DateTime Timestamp, JsonElement? Data);
