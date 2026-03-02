using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ShoppingList"/> entity.
/// Defines property constraints, the Space relationship, and indexes
/// including a unique composite on <c>(SpaceId, NormalizedName)</c>.
/// </summary>
public sealed class ShoppingListConfiguration : BaseEntityConfiguration<ShoppingList>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.Property(e => e.SpaceId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(e => e.Space)
            .WithMany(s => s.ShoppingLists)
            .HasForeignKey(e => e.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SpaceId);

        builder.HasIndex(e => new { e.SpaceId, e.NormalizedName })
            .IsUnique();
    }
}
