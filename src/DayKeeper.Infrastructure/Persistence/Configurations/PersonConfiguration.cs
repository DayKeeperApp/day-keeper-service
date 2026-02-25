using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Person"/> entity.
/// Defines property constraints, the Space relationship, and indexes
/// including a unique composite on <c>(SpaceId, NormalizedFullName)</c>.
/// </summary>
public sealed class PersonConfiguration : BaseEntityConfiguration<Person>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Person> builder)
    {
        builder.Property(e => e.SpaceId)
            .IsRequired();

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedFullName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Notes)
            .IsRequired(false);

        builder.HasOne(e => e.Space)
            .WithMany(s => s.People)
            .HasForeignKey(e => e.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SpaceId);

        builder.HasIndex(e => new { e.SpaceId, e.NormalizedFullName })
            .IsUnique();
    }
}
