namespace DayKeeper.Domain.Entities;

/// <summary>
/// A named calendar within a <see cref="Space"/>.
/// Each space can contain multiple calendars; at most one may be marked as default.
/// </summary>
public class Calendar : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>Display name for the calendar.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Hex color code (e.g. "#FF5733") used to visually distinguish this calendar.</summary>
    public required string Color { get; set; }

    /// <summary>
    /// Indicates whether this is the default calendar for the owning space.
    /// At most one calendar per space should be marked as default.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>Navigation to the owning space.</summary>
    public Space Space { get; set; } = null!;
}
