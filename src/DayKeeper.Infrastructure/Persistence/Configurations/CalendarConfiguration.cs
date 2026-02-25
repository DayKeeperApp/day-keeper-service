using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Calendar"/> entity.
/// Defines property constraints, the Space relationship, and indexes
/// including a unique composite on <c>(SpaceId, NormalizedName)</c>.
/// </summary>
public sealed class CalendarConfiguration : BaseEntityConfiguration<Calendar>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Calendar> builder)
    {
        builder.Property(e => e.SpaceId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Color)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(e => e.IsDefault)
            .IsRequired();

        builder.HasOne(e => e.Space)
            .WithMany(s => s.Calendars)
            .HasForeignKey(e => e.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SpaceId);

        builder.HasIndex(e => new { e.SpaceId, e.NormalizedName })
            .IsUnique();
    }
}
