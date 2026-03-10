using System.Globalization;
using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="DeviceNotificationPreference"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class DeviceNotificationPreferenceMutations
{
    /// <summary>Partially updates notification preferences for a device.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<DeviceNotificationPreference> UpdateDeviceNotificationPreferenceAsync(
        Guid deviceId,
        bool? dndEnabled,
        string? dndStartTime,
        string? dndEndTime,
        ReminderLeadTime? defaultReminderLeadTime,
        NotificationSound? notificationSound,
        bool? notifyEvents,
        bool? notifyTasks,
        bool? notifyLists,
        bool? notifyPeople,
        IDeviceNotificationPreferenceService service,
        CancellationToken cancellationToken)
    {
        TimeOnly? parsedStart = null;
        TimeOnly? parsedEnd = null;

        try
        {
            if (dndStartTime is not null)
            {
                parsedStart = TimeOnly.ParseExact(dndStartTime, "HH:mm", CultureInfo.InvariantCulture);
            }

            if (dndEndTime is not null)
            {
                parsedEnd = TimeOnly.ParseExact(dndEndTime, "HH:mm", CultureInfo.InvariantCulture);
            }
        }
        catch (FormatException)
        {
            throw new InputValidationException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["dndStartTime"] = ["DND times must be in HH:mm format (24-hour UTC)."],
                ["dndEndTime"] = ["DND times must be in HH:mm format (24-hour UTC)."],
            });
        }

        return service.UpdateAsync(
            deviceId, dndEnabled, parsedStart, parsedEnd,
            defaultReminderLeadTime, notificationSound,
            notifyEvents, notifyTasks, notifyLists, notifyPeople,
            cancellationToken);
    }
}
