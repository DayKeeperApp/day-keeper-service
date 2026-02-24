using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// An organizational container that groups related data within a tenant.
/// </summary>
public class Space : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Tenant"/>.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Display name for the space.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Determines the behavior and constraints of this space.</summary>
    public SpaceType SpaceType { get; set; }

    /// <summary>Navigation to the owning tenant.</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Users who are members of this space.</summary>
    public ICollection<SpaceMembership> Memberships { get; set; } = [];

    /// <summary>Calendars belonging to this space.</summary>
    public ICollection<Calendar> Calendars { get; set; } = [];
}
