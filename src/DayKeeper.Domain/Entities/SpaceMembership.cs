using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Associates a <see cref="User"/> with a <see cref="Space"/> and defines their role.
/// </summary>
public class SpaceMembership : BaseEntity
{
    /// <summary>Foreign key to the <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>Foreign key to the <see cref="User"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>The role this user holds in the space.</summary>
    public SpaceRole Role { get; set; }

    /// <summary>Navigation to the space.</summary>
    public Space Space { get; set; } = null!;

    /// <summary>Navigation to the user.</summary>
    public User User { get; set; } = null!;
}
