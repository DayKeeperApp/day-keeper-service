namespace DayKeeper.Domain.Entities;

/// <summary>
/// A significant date associated with a <see cref="Person"/> (e.g. birthday or anniversary).
/// Optionally linked to an <see cref="EventType"/> for calendar categorization.
/// </summary>
public class ImportantDate : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Person"/>.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Descriptive label for the date (e.g. "Birthday", "Anniversary").</summary>
    public required string Label { get; set; }

    /// <summary>The calendar date of the important event.</summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Optional foreign key to an <see cref="EventType"/> for categorization.
    /// <c>null</c> if no event type is assigned.
    /// </summary>
    public Guid? EventTypeId { get; set; }

    /// <summary>Navigation to the owning person.</summary>
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Navigation to the associated event type.
    /// <c>null</c> if no event type is assigned.
    /// </summary>
    public EventType? EventType { get; set; }
}
