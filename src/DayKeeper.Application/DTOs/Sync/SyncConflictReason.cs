namespace DayKeeper.Application.DTOs.Sync;

/// <summary>Categorises why a push entry was rejected as a conflict.</summary>
public enum SyncConflictReason
{
    TimestampConflict,
    DuplicateEntity,
}
