using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Attachment"/> entity.
/// Defines property constraints, parent entity relationships,
/// indexes, and a check constraint ensuring exactly one parent FK is populated.
/// </summary>
public sealed class AttachmentConfiguration : BaseEntityConfiguration<Attachment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Attachment> builder)
    {
        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.CalendarEventId)
            .IsRequired(false);

        builder.Property(e => e.TaskItemId)
            .IsRequired(false);

        builder.Property(e => e.PersonId)
            .IsRequired(false);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.FileSize)
            .IsRequired();

        builder.Property(e => e.StoragePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CalendarEvent)
            .WithMany(e => e.Attachments)
            .HasForeignKey(e => e.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TaskItem)
            .WithMany(e => e.Attachments)
            .HasForeignKey(e => e.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany(e => e.Attachments)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CalendarEventId);
        builder.HasIndex(e => e.TaskItemId);
        builder.HasIndex(e => e.PersonId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Attachment_SingleParent",
            """
            (CASE WHEN "CalendarEventId" IS NOT NULL THEN 1 ELSE 0 END
            + CASE WHEN "TaskItemId" IS NOT NULL THEN 1 ELSE 0 END
            + CASE WHEN "PersonId" IS NOT NULL THEN 1 ELSE 0 END) = 1
            """));
    }
}
