using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Device"/> entity.
/// </summary>
public sealed class DeviceConfiguration : BaseEntityConfiguration<Device>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Device> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.DeviceName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Platform)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.FcmToken)
            .IsRequired()
            .HasMaxLength(4096);

        builder.Property(e => e.LastSyncAt)
            .IsRequired(false);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.UserId);

        builder.HasIndex(e => e.FcmToken)
            .IsUnique();
    }
}
