using System.Text.Json;

namespace DayKeeper.UserEmulator.Client;

public sealed record SyncPushEntry(int EntityType, Guid EntityId, int Operation, DateTime Timestamp, JsonElement? Data);
