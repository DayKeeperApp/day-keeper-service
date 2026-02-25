using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="SpaceMembership"/> entity.
/// Defines property constraints, Space and User relationships, and indexes
/// including a unique composite on <c>(SpaceId, UserId)</c>.
/// </summary>
public sealed class SpaceMembershipConfiguration : BaseEntityConfiguration<SpaceMembership>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SpaceMembership> builder)
    {
        builder.Property(e => e.SpaceId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.HasOne(e => e.Space)
            .WithMany(s => s.Memberships)
            .HasForeignKey(e => e.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(u => u.SpaceMemberships)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.SpaceId, e.UserId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
