namespace DayKeeper.Domain.Entities;

/// <summary>
/// A shopping list within a <see cref="Space"/>.
/// Contains <see cref="ListItem"/> entries representing items to purchase.
/// </summary>
public class ShoppingList : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>Display name for the shopping list.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>Navigation to the owning space.</summary>
    public Space Space { get; set; } = null!;

    /// <summary>Items belonging to this shopping list.</summary>
    public ICollection<ListItem> ListItems { get; set; } = [];
}
