using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Categorizes events (e.g. birthday, holiday, appointment).
/// System-defined event types have a <c>null</c> <see cref="TenantId"/> and are
/// available to all tenants; user-created types are scoped to a single tenant.
/// </summary>
public class EventType : BaseEntity, IOptionalTenantScoped
{
    /// <summary>
    /// Foreign key to the owning <see cref="Tenant"/>.
    /// <c>null</c> for system-defined event types.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Display name for the event type.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Hex color code (e.g. "#4A90D9") used to visually distinguish this event type.</summary>
    public required string Color { get; set; }

    /// <summary>
    /// Optional icon identifier (e.g. a Material Icon name or emoji).
    /// <c>null</c> if no icon is assigned.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates whether this is a system-defined event type.
    /// System types have no owning tenant and are available to all tenants.
    /// </summary>
    public bool IsSystem => !TenantId.HasValue;

    /// <summary>
    /// Navigation to the owning tenant.
    /// <c>null</c> for system-defined event types.
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>Events categorized with this event type.</summary>
    public ICollection<CalendarEvent> Events { get; set; } = [];
}
