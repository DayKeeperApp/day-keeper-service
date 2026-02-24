using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="User"/> entity.
/// Defines property constraints, the Tenant relationship, and indexes
/// on <c>TenantId</c> and a unique composite on <c>(TenantId, Email)</c>.
/// </summary>
public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.Timezone)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.WeekStart)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Locale)
            .IsRequired(false)
            .HasMaxLength(16);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .IsUnique();
    }
}
