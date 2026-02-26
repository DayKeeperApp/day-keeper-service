using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="User"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class UserQueries
{
    /// <summary>Paginated list of users.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers(DayKeeperDbContext dbContext)
    {
        return dbContext.Set<User>().OrderBy(u => u.DisplayName);
    }

    /// <summary>Retrieves a single user by its unique identifier.</summary>
    public Task<User?> GetUserById(
        Guid id,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return userService.GetUserAsync(id, cancellationToken);
    }

    /// <summary>Retrieves a single user by email within a tenant.</summary>
    public Task<User?> GetUserByEmail(
        Guid tenantId,
        string email,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return userService.GetUserByEmailAsync(tenantId, email, cancellationToken);
    }
}
