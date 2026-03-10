using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="DeviceNotificationPreference"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class DeviceNotificationPreferenceQueries
{
    /// <summary>Retrieves notification preferences for a specific device.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<DeviceNotificationPreference> GetDeviceNotificationPreferenceByDeviceId(
        Guid deviceId, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<DeviceNotificationPreference>()
            .Where(p => p.DeviceId == deviceId);
    }
}
