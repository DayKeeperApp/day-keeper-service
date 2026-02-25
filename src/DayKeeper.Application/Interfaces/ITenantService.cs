using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing tenants.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="Tenant"/> entities.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant with the specified name and slug.
    /// </summary>
    /// <param name="name">The display name for the tenant.</param>
    /// <param name="slug">The unique slug used in URLs and lookups.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created tenant.</returns>
    /// <exception cref="DuplicateSlugException">A tenant with the same slug already exists.</exception>
    Task<Tenant> CreateTenantAsync(
        string name,
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant by its unique identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The tenant if found; otherwise, <c>null</c>.</returns>
    Task<Tenant?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant by its unique slug.
    /// </summary>
    /// <param name="slug">The slug to look up (will be normalized).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The tenant if found; otherwise, <c>null</c>.</returns>
    Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name and/or slug of an existing tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to update.</param>
    /// <param name="name">The new display name, or <c>null</c> to leave unchanged.</param>
    /// <param name="slug">The new slug, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated tenant.</returns>
    /// <exception cref="EntityNotFoundException">The tenant does not exist.</exception>
    /// <exception cref="DuplicateSlugException">The new slug conflicts with an existing tenant.</exception>
    Task<Tenant> UpdateTenantAsync(
        Guid tenantId,
        string? name,
        string? slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a tenant. Child entities are cascade-deleted at the database level.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the tenant was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
