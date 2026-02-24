using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Entities;

/// <summary>
/// A way to contact a <see cref="Person"/> (e.g. phone number or email address).
/// </summary>
public class ContactMethod : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Person"/>.</summary>
    public Guid PersonId { get; set; }

    /// <summary>The kind of contact method.</summary>
    public ContactMethodType Type { get; set; }

    /// <summary>The contact value (e.g. a phone number or email address).</summary>
    public required string Value { get; set; }

    /// <summary>
    /// Optional label describing the purpose (e.g. "Work", "Home").
    /// <c>null</c> if no label is assigned.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>Indicates whether this is the primary contact method of its type for the person.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Navigation to the owning person.</summary>
    public Person Person { get; set; } = null!;
}
