using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ListItem"/> entity.
/// Defines property constraints, the ShoppingList relationship, and indexes
/// including a composite on <c>(ShoppingListId, IsChecked, SortOrder)</c>.
/// </summary>
public sealed class ListItemConfiguration : BaseEntityConfiguration<ListItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ListItem> builder)
    {
        builder.Property(e => e.ShoppingListId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(e => e.Unit)
            .IsRequired(false)
            .HasMaxLength(32);

        builder.Property(e => e.IsChecked)
            .IsRequired();

        builder.Property(e => e.SortOrder)
            .IsRequired();

        builder.HasOne(e => e.ShoppingList)
            .WithMany(s => s.ListItems)
            .HasForeignKey(e => e.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ShoppingListId);

        builder.HasIndex(e => new { e.ShoppingListId, e.IsChecked, e.SortOrder });
    }
}
