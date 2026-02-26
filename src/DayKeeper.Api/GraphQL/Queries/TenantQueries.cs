using DayKeeper.Application.Interfaces;
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
    public Task<Tenant?> GetTenantById(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        return tenantService.GetTenantAsync(id, cancellationToken);
    }

    /// <summary>Retrieves a single tenant by its unique slug.</summary>
    public Task<Tenant?> GetTenantBySlug(
        string slug,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        return tenantService.GetTenantBySlugAsync(slug, cancellationToken);
    }
}
