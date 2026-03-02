namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or rename a shopping list to a name
/// that already exists within the same space.
/// </summary>
public sealed class DuplicateShoppingListNameException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateShoppingListNameException"/> class.
    /// </summary>
    /// <param name="spaceId">The space in which the duplicate was detected.</param>
    /// <param name="normalizedName">The normalized name that collides.</param>
    public DuplicateShoppingListNameException(Guid spaceId, string normalizedName)
        : base($"A shopping list with the name '{normalizedName}' already exists in space '{spaceId}'.")
    {
        SpaceId = spaceId;
        NormalizedName = normalizedName;
    }

    /// <summary>The space in which the duplicate was detected.</summary>
    public Guid SpaceId { get; }

    /// <summary>The normalized name that collides.</summary>
    public string NormalizedName { get; }
}
