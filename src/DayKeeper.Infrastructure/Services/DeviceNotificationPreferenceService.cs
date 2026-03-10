using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IDeviceNotificationPreferenceService"/>.
/// </summary>
public sealed class DeviceNotificationPreferenceService(
    IRepository<DeviceNotificationPreference> repository,
    DbContext dbContext) : IDeviceNotificationPreferenceService
{
    private readonly IRepository<DeviceNotificationPreference> _repository = repository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<DeviceNotificationPreference> GetByDeviceIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var preference = await _dbContext.Set<DeviceNotificationPreference>()
            .FirstOrDefaultAsync(p => p.DeviceId == deviceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(
                nameof(DeviceNotificationPreference),
                $"no preferences found for device '{deviceId}'");

        return preference;
    }

    /// <inheritdoc />
    public async Task<DeviceNotificationPreference> UpdateAsync(
        Guid deviceId,
        bool? dndEnabled,
        TimeOnly? dndStartTime,
        TimeOnly? dndEndTime,
        ReminderLeadTime? defaultReminderLeadTime,
        NotificationSound? notificationSound,
        bool? notifyEvents,
        bool? notifyTasks,
        bool? notifyLists,
        bool? notifyPeople,
        CancellationToken cancellationToken = default)
    {
        var preference = await _dbContext.Set<DeviceNotificationPreference>()
            .FirstOrDefaultAsync(p => p.DeviceId == deviceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(
                nameof(DeviceNotificationPreference),
                $"no preferences found for device '{deviceId}'");

        if (dndEnabled.HasValue)
        {
            preference.DndEnabled = dndEnabled.Value;
        }

        if (dndStartTime.HasValue)
        {
            preference.DndStartTime = dndStartTime.Value;
        }

        if (dndEndTime.HasValue)
        {
            preference.DndEndTime = dndEndTime.Value;
        }

        if (defaultReminderLeadTime.HasValue)
        {
            preference.DefaultReminderLeadTime = defaultReminderLeadTime.Value;
        }

        if (notificationSound.HasValue)
        {
            preference.NotificationSound = notificationSound.Value;
        }

        if (notifyEvents.HasValue)
        {
            preference.NotifyEvents = notifyEvents.Value;
        }

        if (notifyTasks.HasValue)
        {
            preference.NotifyTasks = notifyTasks.Value;
        }

        if (notifyLists.HasValue)
        {
            preference.NotifyLists = notifyLists.Value;
        }

        if (notifyPeople.HasValue)
        {
            preference.NotifyPeople = notifyPeople.Value;
        }

        await _repository.UpdateAsync(preference, cancellationToken)
            .ConfigureAwait(false);

        return preference;
    }
}
