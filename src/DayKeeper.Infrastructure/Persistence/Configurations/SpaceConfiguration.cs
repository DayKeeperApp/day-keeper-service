using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Space"/> entity.
/// Defines property constraints, the Tenant relationship, and indexes
/// including a unique composite on <c>(TenantId, NormalizedName)</c>.
/// </summary>
public sealed class SpaceConfiguration : BaseEntityConfiguration<Space>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Space> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.SpaceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Spaces)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.NormalizedName })
            .IsUnique();
    }
}
