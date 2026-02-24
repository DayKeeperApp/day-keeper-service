namespace DayKeeper.Domain.Entities;

/// <summary>
/// Top-level organizational unit that owns all users and data.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>Display name for the tenant.</summary>
    public required string Name { get; set; }

    /// <summary>Unique, normalized identifier used in URLs and lookups.</summary>
    public required string Slug { get; set; }

    /// <summary>Users belonging to this tenant.</summary>
    public ICollection<User> Users { get; set; } = [];

    /// <summary>Spaces belonging to this tenant.</summary>
    public ICollection<Space> Spaces { get; set; } = [];

    /// <summary>User-created event types belonging to this tenant.</summary>
    public ICollection<EventType> EventTypes { get; set; } = [];

    /// <summary>User-created categories belonging to this tenant.</summary>
    public ICollection<Category> Categories { get; set; } = [];
}
