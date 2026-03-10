using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="DeviceNotificationPreference"/> entity.
/// </summary>
public sealed class DeviceNotificationPreferenceConfiguration
    : BaseEntityConfiguration<DeviceNotificationPreference>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DeviceNotificationPreference> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.DeviceId)
            .IsRequired();

        builder.Property(e => e.DndEnabled)
            .IsRequired();

        builder.Property(e => e.DndStartTime)
            .IsRequired();

        builder.Property(e => e.DndEndTime)
            .IsRequired();

        builder.Property(e => e.DefaultReminderLeadTime)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.NotificationSound)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.NotifyEvents)
            .IsRequired();

        builder.Property(e => e.NotifyTasks)
            .IsRequired();

        builder.Property(e => e.NotifyLists)
            .IsRequired();

        builder.Property(e => e.NotifyPeople)
            .IsRequired();

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Device)
            .WithOne(d => d.NotificationPreference)
            .HasForeignKey<DeviceNotificationPreference>(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.DeviceId)
            .IsUnique();
    }
}
