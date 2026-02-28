namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or rename a project to a name
/// that already exists within the same space.
/// </summary>
public sealed class DuplicateProjectNameException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateProjectNameException"/> class.
    /// </summary>
    /// <param name="spaceId">The space in which the duplicate was detected.</param>
    /// <param name="normalizedName">The normalized name that collides.</param>
    public DuplicateProjectNameException(Guid spaceId, string normalizedName)
        : base($"A project with the name '{normalizedName}' already exists in space '{spaceId}'.")
    {
        SpaceId = spaceId;
        NormalizedName = normalizedName;
    }

    /// <summary>The space in which the duplicate was detected.</summary>
    public Guid SpaceId { get; }

    /// <summary>The normalized name that collides.</summary>
    public string NormalizedName { get; }
}
