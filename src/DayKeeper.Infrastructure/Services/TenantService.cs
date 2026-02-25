using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ITenantService"/>.
/// Orchestrates business rules for tenant management
/// using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class TenantService(
    IRepository<Tenant> tenantRepository,
    DbContext dbContext) : ITenantService
{
    private readonly IRepository<Tenant> _tenantRepository = tenantRepository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Tenant> CreateTenantAsync(
        string name,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var slugExists = await _dbContext.Set<Tenant>()
            .AnyAsync(t => t.Slug == normalizedSlug, cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
        {
            throw new DuplicateSlugException(normalizedSlug);
        }

        var tenant = new Tenant
        {
            Name = name.Trim(),
            Slug = normalizedSlug,
        };

        return await _tenantRepository.AddAsync(tenant, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetTenantBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return await _dbContext.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Slug == normalizedSlug, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Tenant> UpdateTenantAsync(
        Guid tenantId,
        string? name,
        string? slug,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Tenant), tenantId);

        if (name is not null)
        {
            tenant.Name = name.Trim();
        }

        if (slug is not null)
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();

            if (!string.Equals(normalizedSlug, tenant.Slug, StringComparison.Ordinal))
            {
                var slugExists = await _dbContext.Set<Tenant>()
                    .AnyAsync(t => t.Slug == normalizedSlug && t.Id != tenantId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (slugExists)
                {
                    throw new DuplicateSlugException(normalizedSlug);
                }

                tenant.Slug = normalizedSlug;
            }
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken)
            .ConfigureAwait(false);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.DeleteAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);
    }
}
