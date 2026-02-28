using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>A single change entry returned by pull (metadata only, no entity body).</summary>
public sealed record SyncChangeEntry(
    long Id,
    ChangeLogEntityType EntityType,
    Guid EntityId,
    ChangeOperation Operation,
    DateTime Timestamp);
