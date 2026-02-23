namespace DayKeeper.Domain.Enums;

/// <summary>
/// Classifies the purpose and behavior of a <see cref="Entities.Space"/>.
/// </summary>
public enum SpaceType
{
    /// <summary>Auto-created private space for a single user.</summary>
    Personal = 0,

    /// <summary>Collaborative space shared among multiple users.</summary>
    Shared = 1,

    /// <summary>Read-only system-managed space.</summary>
    System = 2,
}
