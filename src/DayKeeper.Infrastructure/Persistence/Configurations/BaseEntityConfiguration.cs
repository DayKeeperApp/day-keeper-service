using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base EF Core configuration for all entities derived from <see cref="BaseEntity"/>.
/// Configures identity, audit timestamps, and soft-delete column mapping.
/// Entity-specific configurations inherit from this class and implement
/// <see cref="ConfigureEntity"/>.
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        ConfigureBaseEntity(builder);
        ConfigureEntity(builder);
    }

    /// <summary>
    /// Override in derived classes to add entity-specific configuration
    /// (properties, relationships, indexes, etc.).
    /// </summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);

    private static void ConfigureBaseEntity(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        builder.Ignore(e => e.IsDeleted);
    }
}
