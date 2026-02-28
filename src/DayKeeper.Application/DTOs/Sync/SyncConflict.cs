using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Describes a single LWW conflict detected during push.</summary>
public sealed record SyncConflict(
    ChangeLogEntityType EntityType,
    Guid EntityId,
    DateTime ClientTimestamp,
    DateTime ServerTimestamp);
