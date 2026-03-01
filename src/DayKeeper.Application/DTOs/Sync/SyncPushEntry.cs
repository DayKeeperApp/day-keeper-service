using System.Text.Json;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>
/// A single entity change submitted by the client via push.
/// <see cref="Data"/> must be populated for Create/Update operations
/// and should be omitted for Delete operations.
/// </summary>
public sealed record SyncPushEntry(
    ChangeLogEntityType EntityType,
    Guid EntityId,
    ChangeOperation Operation,
    DateTime Timestamp,
    JsonElement? Data);
