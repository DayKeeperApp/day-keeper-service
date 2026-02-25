using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ChangeLog"/> entity.
/// Configures the auto-increment primary key, property constraints,
/// enum-to-string conversions, and indexes for efficient sync queries.
/// </summary>
public sealed class ChangeLogConfiguration : IEntityTypeConfiguration<ChangeLog>
{
    public void Configure(EntityTypeBuilder<ChangeLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.Operation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.TenantId)
            .IsRequired(false);

        builder.Property(e => e.SpaceId)
            .IsRequired(false);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        // Primary sync query: "all changes for tenant X since cursor Y"
        builder.HasIndex(e => new { e.TenantId, e.Id });

        // Space-scoped sync query
        builder.HasIndex(e => new { e.SpaceId, e.Id });

        // Entity change history lookup
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
