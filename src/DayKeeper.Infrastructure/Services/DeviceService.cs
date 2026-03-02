using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IDeviceService"/>.
/// </summary>
public sealed class DeviceService(
    IRepository<Device> deviceRepository,
    IRepository<User> userRepository,
    DbContext dbContext) : IDeviceService
{
    private readonly IRepository<Device> _deviceRepository = deviceRepository;
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Device> CreateDeviceAsync(
        Guid userId,
        string deviceName,
        DevicePlatform platform,
        string fcmToken,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        var tokenExists = await _dbContext.Set<Device>()
            .AnyAsync(d => d.FcmToken == fcmToken, cancellationToken)
            .ConfigureAwait(false);

        if (tokenExists)
        {
            throw new DuplicateFcmTokenException(fcmToken);
        }

        var device = new Device
        {
            TenantId = user.TenantId,
            UserId = userId,
            DeviceName = deviceName.Trim(),
            Platform = platform,
            FcmToken = fcmToken,
        };

        return await _deviceRepository.AddAsync(device, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Device?> GetDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Device> UpdateDeviceAsync(
        Guid deviceId,
        string? deviceName,
        string? fcmToken,
        DateTime? lastSyncAt,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Device), deviceId);

        if (deviceName is not null)
        {
            device.DeviceName = deviceName.Trim();
        }

        if (fcmToken is not null)
        {
            if (!string.Equals(fcmToken, device.FcmToken, StringComparison.Ordinal))
            {
                var tokenExists = await _dbContext.Set<Device>()
                    .AnyAsync(d => d.FcmToken == fcmToken && d.Id != deviceId, cancellationToken)
                    .ConfigureAwait(false);

                if (tokenExists)
                {
                    throw new DuplicateFcmTokenException(fcmToken);
                }
            }

            device.FcmToken = fcmToken;
        }

        if (lastSyncAt.HasValue)
        {
            device.LastSyncAt = lastSyncAt.Value;
        }

        await _deviceRepository.UpdateAsync(device, cancellationToken)
            .ConfigureAwait(false);

        return device;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return await _deviceRepository.DeleteAsync(deviceId, cancellationToken)
            .ConfigureAwait(false);
    }
}
