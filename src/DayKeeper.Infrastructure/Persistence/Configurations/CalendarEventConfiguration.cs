using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="CalendarEvent"/> entity.
/// Defines property constraints, Calendar and optional EventType relationships,
/// and indexes including a composite on <c>(CalendarId, StartAt)</c> for
/// efficient event range queries.
/// </summary>
public sealed class CalendarEventConfiguration : BaseEntityConfiguration<CalendarEvent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CalendarEvent> builder)
    {
        ConfigureProperties(builder);
        ConfigureRelationships(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureProperties(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.Property(e => e.CalendarId)
            .IsRequired();

        builder.Property(e => e.EventTypeId)
            .IsRequired(false);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Description)
            .IsRequired(false);

        builder.Property(e => e.IsAllDay)
            .IsRequired();

        builder.Property(e => e.StartAt)
            .IsRequired();

        builder.Property(e => e.EndAt)
            .IsRequired();

        builder.Property(e => e.StartDate)
            .IsRequired(false);

        builder.Property(e => e.EndDate)
            .IsRequired(false);

        builder.Property(e => e.Timezone)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.RecurrenceRule)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(e => e.RecurrenceEndAt)
            .IsRequired(false);

        builder.Property(e => e.Location)
            .IsRequired(false)
            .HasMaxLength(512);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasOne(e => e.Calendar)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventType)
            .WithMany(et => et.Events)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasIndex(e => e.CalendarId)
            .HasDatabaseName("IX_CalendarEvent_CalendarId");

        builder.HasIndex(e => e.EventTypeId);

        builder.HasIndex(e => new { e.CalendarId, e.StartAt });

        builder.HasIndex("CalendarId")
            .HasFilter("\"RecurrenceRule\" IS NOT NULL")
            .HasDatabaseName("IX_CalendarEvent_CalendarId_Recurring");
    }
}
