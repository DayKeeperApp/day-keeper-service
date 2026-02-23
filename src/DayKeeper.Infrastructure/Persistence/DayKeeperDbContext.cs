using System.Linq.Expressions;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Persistence;

public sealed class DayKeeperDbContext : DbContext
{
    public DayKeeperDbContext(DbContextOptions<DayKeeperDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DayKeeperDbContext).Assembly);

        ApplySoftDeleteQueryFilter(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var deletedAtProperty = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
            var nullConstant = Expression.Constant(null, typeof(DateTime?));
            var comparison = Expression.Equal(deletedAtProperty, nullConstant);
            var lambda = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
