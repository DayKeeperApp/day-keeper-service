using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using HotChocolate.Data;

namespace DayKeeper.Api.GraphQL.Queries;

/// <summary>
/// Query resolvers for <see cref="Tenant"/> entities.
/// </summary>
[ExtendObjectType(typeof(Query))]
public sealed class TenantQueries
{
    /// <summary>Paginated list of tenants.</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Tenant> GetTenants(DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Tenant>().OrderBy(t => t.Name);
    }

    /// <summary>Retrieves a single tenant by its unique identifier.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Tenant> GetTenantById(Guid id, DayKeeperDbContext dbContext)
    {
        return dbContext.Set<Tenant>().Where(t => t.Id == id);
    }

    /// <summary>Retrieves a single tenant by its unique slug.</summary>
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Tenant> GetTenantBySlug(string slug, DayKeeperDbContext dbContext)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return dbContext.Set<Tenant>().Where(t => t.Slug == normalizedSlug);
    }
}
