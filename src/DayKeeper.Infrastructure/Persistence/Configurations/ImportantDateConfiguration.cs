using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ImportantDate"/> entity.
/// Defines property constraints, Person and EventType relationships,
/// and indexes on <c>PersonId</c> and <c>EventTypeId</c>.
/// </summary>
public sealed class ImportantDateConfiguration : BaseEntityConfiguration<ImportantDate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ImportantDate> builder)
    {
        builder.Property(e => e.PersonId)
            .IsRequired();

        builder.Property(e => e.Label)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.EventTypeId)
            .IsRequired(false);

        builder.HasOne(e => e.Person)
            .WithMany(p => p.ImportantDates)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.PersonId);

        builder.HasIndex(e => e.EventTypeId);
    }
}
