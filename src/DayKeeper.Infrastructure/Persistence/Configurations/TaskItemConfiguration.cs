using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="TaskItem"/> entity.
/// Defines property constraints, Space and Project relationships,
/// enum-to-string conversions, and indexes including a composite on
/// <c>(SpaceId, Status, DueAt)</c> for task listing queries.
/// </summary>
public sealed class TaskItemConfiguration : BaseEntityConfiguration<TaskItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TaskItem> builder)
    {
        builder.Property(e => e.SpaceId)
            .IsRequired();

        builder.Property(e => e.ProjectId)
            .IsRequired(false);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Description)
            .IsRequired(false);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.DueAt)
            .IsRequired(false);

        builder.Property(e => e.DueDate)
            .IsRequired(false);

        builder.Property(e => e.RecurrenceRule)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(e => e.CompletedAt)
            .IsRequired(false);

        builder.HasOne(e => e.Space)
            .WithMany(s => s.TaskItems)
            .HasForeignKey(e => e.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Project)
            .WithMany(p => p.TaskItems)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SpaceId);

        builder.HasIndex(e => e.ProjectId);

        builder.HasIndex(e => new { e.SpaceId, e.Status, e.DueAt });
    }
}
