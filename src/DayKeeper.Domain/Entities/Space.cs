using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// An organizational container that groups related data within a tenant.
/// System spaces (<see cref="TenantId"/> is <c>null</c>) are visible to all tenants.
/// </summary>
public class Space : BaseEntity, IOptionalTenantScoped
{
    /// <summary>
    /// Foreign key to the owning <see cref="Tenant"/>.
    /// <c>null</c> for system-defined spaces.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name for the space.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Determines the behavior and constraints of this space.</summary>
    public SpaceType SpaceType { get; set; }

    /// <summary>
    /// Indicates whether this is a system-defined space.
    /// System spaces have no owning tenant and are available to all tenants.
    /// </summary>
    public bool IsSystem => !TenantId.HasValue;

    /// <summary>Navigation to the owning tenant. <c>null</c> for system-defined spaces.</summary>
    public Tenant? Tenant { get; set; }

    /// <summary>Users who are members of this space.</summary>
    public ICollection<SpaceMembership> Memberships { get; set; } = [];

    /// <summary>Calendars belonging to this space.</summary>
    public ICollection<Calendar> Calendars { get; set; } = [];

    /// <summary>People (contacts) belonging to this space.</summary>
    public ICollection<Person> People { get; set; } = [];

    /// <summary>Projects belonging to this space.</summary>
    public ICollection<Project> Projects { get; set; } = [];

    /// <summary>Tasks belonging to this space.</summary>
    public ICollection<TaskItem> TaskItems { get; set; } = [];

    /// <summary>Shopping lists belonging to this space.</summary>
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
}
