namespace DayKeeper.Domain.Entities;

/// <summary>
/// An individual item on a <see cref="ShoppingList"/>.
/// Tracks the item name, quantity, unit of measure, checked status, and display order.
/// </summary>
public class ListItem : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="ShoppingList"/>.</summary>
    public Guid ShoppingListId { get; set; }

    /// <summary>Name of the item (e.g. "Milk", "Bread").</summary>
    public required string Name { get; set; }

    /// <summary>Quantity of the item to purchase.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Optional unit of measure (e.g. "oz", "lbs", "cups").
    /// <c>null</c> if no unit is specified.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>Indicates whether the item has been checked off the list.</summary>
    public bool IsChecked { get; set; }

    /// <summary>Zero-based position used to determine display order within the shopping list.</summary>
    public int SortOrder { get; set; }

    /// <summary>Navigation to the owning shopping list.</summary>
    public ShoppingList ShoppingList { get; set; } = null!;
}
