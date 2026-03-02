using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Interfaces;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// Represents a physical or virtual device registered to a <see cref="User"/>
/// for push notification delivery via Firebase Cloud Messaging.
/// </summary>
public class Device : BaseEntity, ITenantScoped
{
    /// <summary>Foreign key to the owning <see cref="Tenant"/>.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Foreign key to the owning <see cref="User"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Human-readable label for the device (e.g. "Pixel 9 Pro").</summary>
    public required string DeviceName { get; set; }

    /// <summary>The operating platform of the device.</summary>
    public DevicePlatform Platform { get; set; }

    /// <summary>Firebase Cloud Messaging registration token. Globally unique per device instance.</summary>
    public required string FcmToken { get; set; }

    /// <summary>
    /// The UTC timestamp of the last successful sync for this device.
    /// <c>null</c> if the device has never synced.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Navigation to the owning tenant.</summary>
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Navigation to the owning user.</summary>
    public User User { get; set; } = null!;
}
