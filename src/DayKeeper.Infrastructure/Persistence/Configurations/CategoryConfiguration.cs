using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Category"/> entity.
/// Defines property constraints, the optional Tenant relationship, and indexes
/// including a unique composite on <c>(TenantId, NormalizedName)</c> with
/// <c>NULLS NOT DISTINCT</c> to enforce uniqueness for system categories.
/// </summary>
public sealed class CategoryConfiguration : BaseEntityConfiguration<Category>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Category> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired(false);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Color)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(e => e.Icon)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Ignore(e => e.IsSystem);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Categories)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.NormalizedName })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}
