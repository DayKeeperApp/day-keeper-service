using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="RecurrenceException"/> entity.
/// Defines property constraints, the required relationship to <see cref="CalendarEvent"/>,
/// and a unique composite index on <c>(CalendarEventId, OriginalStartAt)</c> to enforce
/// one exception per occurrence per series.
/// </summary>
public sealed class RecurrenceExceptionConfiguration : BaseEntityConfiguration<RecurrenceException>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RecurrenceException> builder)
    {
        builder.Property(e => e.CalendarEventId)
            .IsRequired();

        builder.Property(e => e.OriginalStartAt)
            .IsRequired();

        builder.Property(e => e.IsCancelled)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(e => e.Description)
            .IsRequired(false);

        builder.Property(e => e.StartAt)
            .IsRequired(false);

        builder.Property(e => e.EndAt)
            .IsRequired(false);

        builder.Property(e => e.Location)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.HasOne(e => e.CalendarEvent)
            .WithMany(ce => ce.RecurrenceExceptions)
            .HasForeignKey(e => e.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CalendarEventId);

        builder.HasIndex(e => new { e.CalendarEventId, e.OriginalStartAt })
            .IsUnique();
    }
}
