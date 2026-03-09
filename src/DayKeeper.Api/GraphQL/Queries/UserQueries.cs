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
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetUserById(Guid id, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<User>().Where(u => u.Id == id);
    }

    /// <summary>Retrieves a single user by email within a tenant.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetUserByEmail(
        Guid tenantId,
        string email,
        DayKeeperDbContext dbContext)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Set<User>().Where(u => u.TenantId == tenantId && u.Email == normalizedEmail);
    }
}
