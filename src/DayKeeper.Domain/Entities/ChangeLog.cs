using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Append-only record of a data mutation within the system.
/// Each entry captures which entity changed, the type of operation, and the
/// tenant/space scope. The auto-increment <see cref="Id"/> serves as a global
/// monotonic cursor for incremental sync.
/// </summary>
public class ChangeLog
{
    /// <summary>
    /// Auto-incrementing primary key that doubles as a global monotonic sync cursor.
    /// Clients request changes where <c>Id &gt; lastSeenCursor</c> to retrieve
    /// only new mutations.
    /// </summary>
    public long Id { get; set; }

    /// <summary>The type of domain entity that was changed.</summary>
    public required ChangeLogEntityType EntityType { get; set; }

    /// <summary>The unique identifier of the changed entity.</summary>
    public required Guid EntityId { get; set; }

    /// <summary>The type of mutation that occurred.</summary>
    public required ChangeOperation Operation { get; set; }

    /// <summary>
    /// The tenant that owns the changed entity.
    /// <c>null</c> for system-level entities (e.g., system-defined
    /// <see cref="EventType"/> or <see cref="Category"/> records) and
    /// for <see cref="Tenant"/> entities themselves.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// The space that contains the changed entity.
    /// <c>null</c> for entities that are not space-scoped
    /// (e.g., <see cref="Tenant"/>, <see cref="User"/>).
    /// </summary>
    public Guid? SpaceId { get; set; }

    /// <summary>UTC timestamp indicating when the change occurred.</summary>
    public required DateTime Timestamp { get; set; }
}
