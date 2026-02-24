namespace DayKeeper.Domain.Enums;

/// <summary>
/// Classifies the type of a <see cref="Entities.ContactMethod"/>.
/// </summary>
public enum ContactMethodType
{
    /// <summary>A telephone number.</summary>
    Phone = 0,

    /// <summary>An email address.</summary>
    Email = 1,

    /// <summary>A contact method that does not fit other categories.</summary>
    Other = 2,
}
