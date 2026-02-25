using System.Linq.Expressions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Persistence;

public sealed class DayKeeperDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DayKeeperDbContext(
        DbContextOptions<DayKeeperDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Exposes the current tenant ID as a property on the DbContext instance.
    /// EF Core parameterizes member accesses on the DbContext, so this value
    /// becomes a SQL parameter that changes per-request without invalidating
    /// the query plan cache.
    /// </summary>
    public Guid? CurrentTenantId => _tenantContext.CurrentTenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DayKeeperDbContext).Assembly);

        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var clrType = entityType.ClrType;
            var parameter = Expression.Parameter(clrType, "entity");

            // Soft-delete filter: entity.DeletedAt == null
            var deletedAtProperty = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
            var nullDateTimeConstant = Expression.Constant(null, typeof(DateTime?));
            Expression combinedFilter = Expression.Equal(deletedAtProperty, nullDateTimeConstant);

            // Tenant filter for ITenantScoped entities (required TenantId)
            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var tenantFilter = BuildRequiredTenantFilter(parameter);
                combinedFilter = Expression.AndAlso(combinedFilter, tenantFilter);
            }
            // Tenant filter for IOptionalTenantScoped entities (nullable TenantId)
            else if (typeof(IOptionalTenantScoped).IsAssignableFrom(clrType))
            {
                var tenantFilter = BuildOptionalTenantFilter(parameter);
                combinedFilter = Expression.AndAlso(combinedFilter, tenantFilter);
            }

            var lambda = Expression.Lambda(combinedFilter, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Builds: this.CurrentTenantId == null || (Guid?)entity.TenantId == this.CurrentTenantId
    /// </summary>
    private BinaryExpression BuildRequiredTenantFilter(ParameterExpression parameter)
    {
        var currentTenantId = Expression.Property(
            Expression.Constant(this), nameof(CurrentTenantId));
        var nullGuid = Expression.Constant(null, typeof(Guid?));

        // this.CurrentTenantId == null  (bypass when no tenant context)
        var tenantIsNull = Expression.Equal(currentTenantId, nullGuid);

        // (Guid?)entity.TenantId == this.CurrentTenantId
        var entityTenantId = Expression.Convert(
            Expression.Property(parameter, nameof(ITenantScoped.TenantId)),
            typeof(Guid?));
        var tenantMatches = Expression.Equal(entityTenantId, currentTenantId);

        return Expression.OrElse(tenantIsNull, tenantMatches);
    }

    /// <summary>
    /// Builds: this.CurrentTenantId == null || entity.TenantId == this.CurrentTenantId || entity.TenantId == null
    /// </summary>
    private BinaryExpression BuildOptionalTenantFilter(ParameterExpression parameter)
    {
        var currentTenantId = Expression.Property(
            Expression.Constant(this), nameof(CurrentTenantId));
        var nullGuid = Expression.Constant(null, typeof(Guid?));

        // this.CurrentTenantId == null  (bypass when no tenant context)
        var tenantIsNull = Expression.Equal(currentTenantId, nullGuid);

        // entity.TenantId == this.CurrentTenantId
        var entityTenantId = Expression.Property(
            parameter, nameof(IOptionalTenantScoped.TenantId));
        var tenantMatches = Expression.Equal(entityTenantId, currentTenantId);

        // entity.TenantId == null  (system-defined records visible to all)
        var entityTenantIsNull = Expression.Equal(entityTenantId, nullGuid);

        return Expression.OrElse(
            tenantIsNull,
            Expression.OrElse(tenantMatches, entityTenantIsNull));
    }
}
