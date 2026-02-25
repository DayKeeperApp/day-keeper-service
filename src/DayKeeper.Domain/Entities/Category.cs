using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A label used to classify <see cref="TaskItem"/> entities (e.g. "Errands", "Health").
/// System-defined categories have a <c>null</c> <see cref="TenantId"/> and are
/// available to all tenants; user-created categories are scoped to a single tenant.
/// </summary>
public class Category : BaseEntity, IOptionalTenantScoped
{
    /// <summary>
    /// Foreign key to the owning <see cref="Tenant"/>.
    /// <c>null</c> for system-defined categories.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name for the category.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Hex color code (e.g. "#34C759") used to visually distinguish this category.</summary>
    public required string Color { get; set; }

    /// <summary>
    /// Optional icon identifier (e.g. a Material Icon name or emoji).
    /// <c>null</c> if no icon is assigned.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates whether this is a system-defined category.
    /// System categories have no owning tenant and are available to all tenants.
    /// </summary>
    public bool IsSystem => !TenantId.HasValue;

    /// <summary>
    /// Navigation to the owning tenant.
    /// <c>null</c> for system-defined categories.
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>Task-category associations for this category.</summary>
    public ICollection<TaskCategory> TaskCategories { get; set; } = [];
}
