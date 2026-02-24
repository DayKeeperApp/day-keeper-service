namespace DayKeeper.Domain.Entities;

/// <summary>
/// A contact or person of interest within a <see cref="Space"/>.
/// Stores name, optional notes, and related contact information.
/// </summary>
public class Person : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>First name of the person.</summary>
    public required string FirstName { get; set; }

    /// <summary>Last name of the person.</summary>
    public required string LastName { get; set; }

    /// <summary>Lowercased, trimmed full name used for uniqueness checks and lookups.</summary>
    public required string NormalizedFullName { get; set; }

    /// <summary>
    /// Optional free-text notes about the person.
    /// <c>null</c> if no notes have been recorded.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>Navigation to the owning space.</summary>
    public Space Space { get; set; } = null!;

    /// <summary>Contact methods associated with this person.</summary>
    public ICollection<ContactMethod> ContactMethods { get; set; } = [];

    /// <summary>Addresses associated with this person.</summary>
    public ICollection<Address> Addresses { get; set; } = [];

    /// <summary>Important dates associated with this person.</summary>
    public ICollection<ImportantDate> ImportantDates { get; set; } = [];
}
