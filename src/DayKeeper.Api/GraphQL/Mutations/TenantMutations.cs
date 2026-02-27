using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Tenant"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class TenantMutations
{
    /// <summary>Creates a new tenant.</summary>
    [Error<InputValidationException>]
    [Error<DuplicateSlugException>]
    public Task<Tenant> CreateTenantAsync(
        string name,
        string slug,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        return tenantService.CreateTenantAsync(name, slug, cancellationToken);
    }

    /// <summary>Updates an existing tenant.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateSlugException>]
    public Task<Tenant> UpdateTenantAsync(
        Guid id,
        string? name,
        string? slug,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        return tenantService.UpdateTenantAsync(id, name, slug, cancellationToken);
    }

    /// <summary>Soft-deletes a tenant and all its child entities.</summary>
    public Task<bool> DeleteTenantAsync(
        Guid id,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        return tenantService.DeleteTenantAsync(id, cancellationToken);
    }
}
