using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Tenant"/> entity.
/// Defines property constraints and a unique index on <see cref="Tenant.Slug"/>.
/// </summary>
public sealed class TenantConfiguration : BaseEntityConfiguration<Tenant>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(e => e.Slug)
            .IsUnique();
    }
}
