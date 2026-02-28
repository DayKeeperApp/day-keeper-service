using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.DTOs.Sync;

/// <summary>A single entity change submitted by the client via push.</summary>
public sealed record SyncPushEntry(
    ChangeLogEntityType EntityType,
    Guid EntityId,
    ChangeOperation Operation,
    DateTime Timestamp);
