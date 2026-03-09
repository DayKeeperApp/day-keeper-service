using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Describes a single conflict detected during push.</summary>
public sealed record SyncConflict(
    ChangeLogEntityType EntityType,
    Guid EntityId,
    SyncConflictReason Reason,
    DateTime? ClientTimestamp,
    DateTime? ServerTimestamp);
