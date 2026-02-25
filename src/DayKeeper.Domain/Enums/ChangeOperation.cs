namespace DayKeeper.Domain.Enums;

/// <summary>
/// The type of data mutation recorded in a <see cref="Entities.ChangeLog"/> entry.
/// </summary>
public enum ChangeOperation
{
    /// <summary>A new entity was created.</summary>
    Created = 0,

    /// <summary>An existing entity was updated.</summary>
    Updated = 1,

    /// <summary>An entity was deleted (soft-deleted).</summary>
    Deleted = 2,
}
