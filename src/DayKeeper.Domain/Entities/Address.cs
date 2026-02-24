namespace DayKeeper.Domain.Entities;

/// <summary>
/// A physical address associated with a <see cref="Person"/>.
/// </summary>
public class Address : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Person"/>.</summary>
    public Guid PersonId { get; set; }

    /// <summary>
    /// Optional label describing the address (e.g. "Home", "Work").
    /// <c>null</c> if no label is assigned.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>Primary street address line.</summary>
    public required string Street1 { get; set; }

    /// <summary>
    /// Secondary street address line (e.g. apartment or suite number).
    /// <c>null</c> if not applicable.
    /// </summary>
    public string? Street2 { get; set; }

    /// <summary>City or locality name.</summary>
    public required string City { get; set; }

    /// <summary>
    /// State, province, or region.
    /// <c>null</c> if not applicable for the country.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code.
    /// <c>null</c> if not applicable for the country.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>Country name or ISO code.</summary>
    public required string Country { get; set; }

    /// <summary>Indicates whether this is the primary address for the person.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Navigation to the owning person.</summary>
    public Person Person { get; set; } = null!;
}
