namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or rename a person to a name
/// that already exists within the same space.
/// </summary>
public sealed class DuplicatePersonNameException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicatePersonNameException"/> class.
    /// </summary>
    /// <param name="spaceId">The space in which the duplicate was detected.</param>
    /// <param name="normalizedFullName">The normalized full name that collides.</param>
    public DuplicatePersonNameException(Guid spaceId, string normalizedFullName)
        : base($"A person with the name '{normalizedFullName}' already exists in space '{spaceId}'.")
    {
        SpaceId = spaceId;
        NormalizedFullName = normalizedFullName;
    }

    /// <summary>The space in which the duplicate was detected.</summary>
    public Guid SpaceId { get; }

    /// <summary>The normalized full name that collides.</summary>
    public string NormalizedFullName { get; }
}
