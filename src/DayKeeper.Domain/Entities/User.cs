using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A person who interacts with the system, scoped to a single tenant.
/// </summary>
public class User : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Tenant"/>.</summary>
    public Guid TenantId { get; set; }

    /// <summary>User's display name.</summary>
    public required string DisplayName { get; set; }

    /// <summary>User's email address.</summary>
    public required string Email { get; set; }

    /// <summary>IANA timezone identifier (e.g. "America/Chicago").</summary>
    public required string Timezone { get; set; }

    /// <summary>Preferred first day of the week.</summary>
    public WeekStart WeekStart { get; set; }

    /// <summary>Locale string for formatting (e.g. "en-US"). Null uses system default.</summary>
    public string? Locale { get; set; }

    /// <summary>Navigation to the owning tenant.</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Space memberships for this user.</summary>
    public ICollection<SpaceMembership> SpaceMemberships { get; set; } = [];
}
