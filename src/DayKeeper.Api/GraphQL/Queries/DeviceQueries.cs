using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Device"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class DeviceQueries
{
    /// <summary>Paginated list of devices, optionally filtered by user.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Device> GetDevices(DayKeeperDbContext dbContext, Guid? userId)
    {
        var query = dbContext.Set<Device>().AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId.Value);
        }

        return query.OrderBy(d => d.DeviceName);
    }

    /// <summary>Retrieves a single device by its unique identifier.</summary>
    public Task<Device?> GetDeviceById(
        Guid id,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        return deviceService.GetDeviceAsync(id, cancellationToken);
    }
}
