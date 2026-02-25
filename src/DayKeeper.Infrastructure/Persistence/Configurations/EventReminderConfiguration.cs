using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="EventReminder"/> entity.
/// Defines property constraints, the CalendarEvent relationship,
/// and an index on <c>CalendarEventId</c>.
/// </summary>
public sealed class EventReminderConfiguration : BaseEntityConfiguration<EventReminder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EventReminder> builder)
    {
        builder.Property(e => e.CalendarEventId)
            .IsRequired();

        builder.Property(e => e.MinutesBefore)
            .IsRequired();

        builder.Property(e => e.Method)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.HasOne(e => e.CalendarEvent)
            .WithMany(e => e.Reminders)
            .HasForeignKey(e => e.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CalendarEventId);
    }
}
