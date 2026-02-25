using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ContactMethod"/> entity.
/// Defines property constraints, the Person relationship, and an index on <c>PersonId</c>.
/// </summary>
public sealed class ContactMethodConfiguration : BaseEntityConfiguration<ContactMethod>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ContactMethod> builder)
    {
        builder.Property(e => e.PersonId)
            .IsRequired();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Label)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Property(e => e.IsPrimary)
            .IsRequired();

        builder.HasOne(e => e.Person)
            .WithMany(p => p.ContactMethods)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonId);
    }
}
