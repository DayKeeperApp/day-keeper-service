using System.Text.Json;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>
/// A single change entry returned by pull.
/// <see cref="Data"/> contains the serialized entity body for Create/Update operations;
/// it is <c>null</c> for Delete operations.
/// </summary>
public sealed record SyncChangeEntry(
    long Id,
    ChangeLogEntityType EntityType,
    Guid EntityId,
    ChangeOperation Operation,
    DateTime Timestamp,
    JsonElement? Data);
