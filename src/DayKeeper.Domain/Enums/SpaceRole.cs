namespace DayKeeper.Domain.Enums;

/// <summary>
/// The role a user holds within a <see cref="Entities.Space"/> via membership.
/// </summary>
public enum SpaceRole
{
    /// <summary>Read-only access to the space.</summary>
    Viewer = 0,

    /// <summary>Can create and edit items in the space.</summary>
    Editor = 1,

    /// <summary>Full control including membership management.</summary>
    Owner = 2,
}
