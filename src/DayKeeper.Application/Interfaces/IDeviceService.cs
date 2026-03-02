using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing user devices registered for push notifications.
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Registers a new device for the specified user.
    /// </summary>
    Task<Device> CreateDeviceAsync(
        Guid userId,
        string deviceName,
        DevicePlatform platform,
        string fcmToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a device by its unique identifier.
    /// </summary>
    Task<Device?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the mutable properties of an existing device registration.
    /// Pass <c>null</c> to leave a field unchanged.
    /// </summary>
    Task<Device> UpdateDeviceAsync(
        Guid deviceId,
        string? deviceName,
        string? fcmToken,
        DateTime? lastSyncAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a device registration.
    /// </summary>
    Task<bool> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
}
