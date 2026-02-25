using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Address"/> entity.
/// Defines property constraints, the Person relationship, and an index on <c>PersonId</c>.
/// </summary>
public sealed class AddressConfiguration : BaseEntityConfiguration<Address>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Address> builder)
    {
        builder.Property(e => e.PersonId)
            .IsRequired();

        builder.Property(e => e.Label)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Property(e => e.Street1)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Street2)
            .IsRequired(false)
            .HasMaxLength(512);

        builder.Property(e => e.City)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.State)
            .IsRequired(false)
            .HasMaxLength(128);

        builder.Property(e => e.PostalCode)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(e => e.Country)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.IsPrimary)
            .IsRequired();

        builder.HasOne(e => e.Person)
            .WithMany(p => p.Addresses)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonId);
    }
}
