using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Device"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class DeviceMutations
{
    /// <summary>Registers a new device for push notification delivery.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateFcmTokenException>]
    public Task<Device> CreateDeviceAsync(
        Guid userId,
        string deviceName,
        DevicePlatform platform,
        string fcmToken,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        return deviceService.CreateDeviceAsync(
            userId, deviceName, platform, fcmToken, cancellationToken);
    }

    /// <summary>Updates an existing device registration.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateFcmTokenException>]
    public Task<Device> UpdateDeviceAsync(
        Guid id,
        string? deviceName,
        string? fcmToken,
        DateTime? lastSyncAt,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        return deviceService.UpdateDeviceAsync(
            id, deviceName, fcmToken, lastSyncAt, cancellationToken);
    }

    /// <summary>Soft-deletes a device registration.</summary>
    public Task<bool> DeleteDeviceAsync(
        Guid id,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        return deviceService.DeleteDeviceAsync(id, cancellationToken);
    }
}
